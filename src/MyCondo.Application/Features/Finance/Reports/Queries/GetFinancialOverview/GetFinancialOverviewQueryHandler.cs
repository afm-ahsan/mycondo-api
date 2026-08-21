using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialOverview;

/// <summary>Composite dashboard figure assembled entirely from other reports' authoritative sources —
/// never a second aggregation path. Income/Expense/Surplus/Receivables/Cash reuse
/// <c>GetTrialBalanceAsync</c>'s cumulative-to-date lines (same source <c>GetFinancialPositionQueryHandler</c>
/// uses); Fixed Deposit principal comes from the FixedDeposit entities themselves (one shared system
/// ledger account cannot distinguish instruments — Template 4); collection performance and expense
/// composition are period-scoped and independently sourced from Invoice/Payment/Expense entities per
/// Template 5's "operational dimensions" rule.</summary>
public sealed class GetFinancialOverviewQueryHandler(
    IFinanceReportRepository reports,
    IAccountMappingRepository accountMappings,
    IFinancialAccountRepository financialAccounts,
    IFixedDepositRepository fixedDeposits,
    IInvoiceRepository invoices,
    IPaymentRepository payments,
    IExpenseRepository expenses,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetFinancialOverviewQuery, FinancialOverviewReportDto>
{
    public async ValueTask<FinancialOverviewReportDto> Handle(GetFinancialOverviewQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateOnly asOfDate = query.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        DateOnly firstOfMonth = new(asOfDate.Year, asOfDate.Month, 1);
        DateOnly fromDate = query.FromDate ?? firstOfMonth;
        DateOnly toDate = query.ToDate ?? asOfDate;

        IReadOnlyList<TrialBalanceAccountLine> lines = await reports.GetTrialBalanceAsync(tenantId, asOfDate, cancellationToken);

        decimal incomeTotal = lines.Where(l => l.Category == AccountCategory.Income).Sum(l => l.TotalCredit - l.TotalDebit);
        decimal expenseTotal = lines.Where(l => l.Category == AccountCategory.Expense).Sum(l => l.TotalDebit - l.TotalCredit);

        decimal receivables = 0m;
        ChartOfAccountId? receivableAccountId = await accountMappings.ResolveAccountIdAsync(
            tenantId, nameof(LedgerAccountType.ResidentReceivable), cancellationToken);
        if (receivableAccountId is ChartOfAccountId receivableId)
        {
            TrialBalanceAccountLine? receivableLine = lines.FirstOrDefault(l => l.ChartOfAccountId == receivableId);
            receivables = receivableLine is null ? 0m : receivableLine.TotalDebit - receivableLine.TotalCredit;
        }

        CashPositionDto cashPosition = await BuildCashPositionAsync(tenantId, lines, cancellationToken);

        decimal fixedDepositPrincipal = await fixedDeposits.GetActivePrincipalTotalAsync(tenantId, cancellationToken);

        InvoiceFinancialAggregate invoiceAggregate = await invoices.GetFinancialAggregateAsync(
            tenantId, buildingId: null, fromDate, toDate, asOfDate, cancellationToken);
        decimal collected = await payments.GetTotalCollectedAsync(tenantId, buildingId: null, fromDate, toDate, cancellationToken);
        CollectionPerformanceDto collectionPerformance = new(invoiceAggregate.TotalBilled, collected);

        IReadOnlyList<ExpenseCategoryActivityLine> compositionActivity =
            await expenses.GetExpenseCompositionByCategoryAsync(tenantId, fromDate, toDate, cancellationToken);
        decimal compositionTotal = compositionActivity.Sum(l => l.Total);
        List<ExpenseCompositionLineDto> expenseComposition = compositionActivity
            .Select(l => new ExpenseCompositionLineDto(
                l.ExpenseCategoryId?.Value, l.CategoryName, l.Total,
                compositionTotal == 0m ? 0m : Math.Round(l.Total / compositionTotal * 100m, 2)))
            .ToList();

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            asOfDate, "Tenant (all accounts)", clock.UtcNow, currentUser.UserId);

        return new FinancialOverviewReportDto(
            metadata, incomeTotal, expenseTotal, incomeTotal - expenseTotal, receivables, cashPosition,
            fixedDepositPrincipal, fromDate, toDate, collectionPerformance, expenseComposition);
    }

    /// <summary>Buckets every cash/bank-touching account (Template 4's <c>FinancialAccount</c>s plus the
    /// tenant's system CashOrBank role account) into Cash-in-Hand/Bank/MFS labels. Any balance still
    /// sitting on the tenant-wide system CashOrBank account that hasn't been attributed to a specific
    /// <see cref="FinancialAccount"/> (legacy pre-Template-4 activity) is folded into
    /// <see cref="CashPositionDto.BankBalance"/> as the closest reasonable default — never dropped from
    /// <see cref="CashPositionDto.AvailableLiquidFunds"/>, which stays reconciled to the same
    /// <c>GetCashAndBankChartOfAccountIdsAsync</c> account set the Cash Flow report uses.</summary>
    private async Task<CashPositionDto> BuildCashPositionAsync(
        Guid tenantId, IReadOnlyList<TrialBalanceAccountLine> lines, CancellationToken cancellationToken)
    {
        Dictionary<ChartOfAccountId, TrialBalanceAccountLine> linesByAccount = lines.ToDictionary(l => l.ChartOfAccountId);

        decimal BalanceOf(Guid chartOfAccountId) =>
            linesByAccount.TryGetValue(new ChartOfAccountId(chartOfAccountId), out TrialBalanceAccountLine? line)
                ? line.TotalDebit - line.TotalCredit
                : 0m;

        List<FinancialAccount> accounts = await financialAccounts.GetAllForTenantAsync(tenantId, cancellationToken);
        IReadOnlyList<Guid> allCashAccountIds = await reports.GetCashAndBankChartOfAccountIdsAsync(tenantId, cancellationToken);

        decimal cashInHand = accounts.Where(a => a.AccountType == FinancialAccountType.Cash).Sum(a => BalanceOf(a.ChartOfAccountId.Value));
        decimal mfsBalance = accounts.Where(a => a.AccountType == FinancialAccountType.MobileFinancialService).Sum(a => BalanceOf(a.ChartOfAccountId.Value));
        decimal bankBalanceFromAccounts = accounts.Where(a => a.AccountType == FinancialAccountType.Bank).Sum(a => BalanceOf(a.ChartOfAccountId.Value));

        HashSet<Guid> attributedAccountIds = accounts.Select(a => a.ChartOfAccountId.Value).ToHashSet();
        decimal unassignedBalance = allCashAccountIds
            .Where(id => !attributedAccountIds.Contains(id))
            .Sum(BalanceOf);

        decimal bankBalance = bankBalanceFromAccounts + unassignedBalance;

        return new CashPositionDto(cashInHand, bankBalance, mfsBalance, cashInHand + bankBalance + mfsBalance);
    }
}
