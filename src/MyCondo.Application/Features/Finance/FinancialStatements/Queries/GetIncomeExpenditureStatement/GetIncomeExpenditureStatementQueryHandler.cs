using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialStatements.Services;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;

/// <summary>Composes Task 2's period GL snapshot into an Income &amp; Expenditure Statement — no second
/// aggregation path over collections, invoices, receipts, expenses, or fixed deposits.
///
/// The present Chart of Accounts cannot truthfully report Expenditure at the ARP workbook's category
/// granularity (Maintenance, Utilities, Gas Purchase, Security, Staff, Administration, ...): every
/// non-FD-interest expense posts through the single tenant-wide <c>LedgerAccountType.OperatingExpense</c>
/// GL account (Task 1, <see cref="FinancialStatementGroup.OperatingExpenses"/> doc comment), and regular
/// vs. additional Service Charge both post through the single <c>ServiceChargeIncome</c> GL role. This
/// handler therefore renders exactly the granularity the GL snapshot exposes and does not fabricate
/// finer groups from <c>ExpenseCategory</c> or invoice-type data — that breakdown becomes a Task 5
/// supporting schedule reconciled back to this statement's <see cref="FinancialStatementGroup.OperatingExpenses"/>
/// total, not a second primary-statement aggregation path.</summary>
public sealed class GetIncomeExpenditureStatementQueryHandler(
    IFinancialStatementReportingService reportingService,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetIncomeExpenditureStatementQuery, IncomeExpenditureStatementDto>
{
    public async ValueTask<IncomeExpenditureStatementDto> Handle(
        GetIncomeExpenditureStatementQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FinancialStatementSnapshot snapshot = await reportingService.GetPeriodSnapshotAsync(
            tenantId, query.StartDate, query.EndDate, query.FundId, cancellationToken);

        IncomeExpenditureStatementSectionDto income = BuildSection(snapshot, AccountCategory.Income);
        IncomeExpenditureStatementSectionDto expenditure = BuildSection(snapshot, AccountCategory.Expense);

        decimal totalIncome = income.Total;
        decimal totalExpenditure = expenditure.Total;

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.StartDate, query.EndDate, query.FundId is null ? "Tenant (all funds)" : "Single fund",
            clock.UtcNow, currentUser.UserId);

        List<UnmappedIncomeExpenditureAccountWarningDto> warnings = snapshot.UnmappedAccountWarnings
            .Where(w => w.Category is AccountCategory.Income or AccountCategory.Expense)
            .Select(w => new UnmappedIncomeExpenditureAccountWarningDto(
                w.ChartOfAccountId, w.Code, w.Name, w.Category, w.EffectiveStatementGroup, w.Amount))
            .ToList();

        return new IncomeExpenditureStatementDto(
            metadata, query.FundId, income, expenditure, totalIncome, totalExpenditure,
            totalIncome - totalExpenditure, warnings);
    }

    private static IncomeExpenditureStatementSectionDto BuildSection(
        FinancialStatementSnapshot snapshot, AccountCategory category)
    {
        List<IncomeExpenditureStatementGroupDto> groups = snapshot.Groups
            .Where(g => g.Category == category)
            .Select(g => new IncomeExpenditureStatementGroupDto(
                g.Group,
                g.Amount,
                g.Accounts
                    .Select(a => new IncomeExpenditureAccountLineDto(a.ChartOfAccountId, a.Code, a.Name, a.Amount))
                    .ToList()))
            .ToList();

        return new IncomeExpenditureStatementSectionDto(groups, groups.Sum(g => g.Amount));
    }
}
