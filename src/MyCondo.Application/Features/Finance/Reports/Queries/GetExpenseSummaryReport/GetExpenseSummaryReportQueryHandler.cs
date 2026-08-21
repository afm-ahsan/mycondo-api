using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseSummaryReport;

/// <summary>See <see cref="ExpenseSummaryReportDto"/> for the LedgerTotal vs. SourceRecordTotal
/// reconciliation semantics. <see cref="IFinanceReportRepository.GetCategoryActivityAsync"/> is reused
/// (not a new single-account query) so this report's ledger total and the Income &amp; Expense report's
/// Expense-side total are computed by the exact same code path — two reports must never independently
/// derive the same ledger figure and risk disagreeing.</summary>
public sealed class GetExpenseSummaryReportQueryHandler(
    IFinanceReportRepository reports,
    IExpenseRepository expenses,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetExpenseSummaryReportQuery, ExpenseSummaryReportDto>
{
    public async ValueTask<ExpenseSummaryReportDto> Handle(GetExpenseSummaryReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<TrialBalanceAccountLine> expenseActivity = await reports.GetCategoryActivityAsync(
            tenantId, AccountCategory.Expense, query.FromDate, query.ToDate, cancellationToken);
        decimal ledgerTotal = expenseActivity.Sum(l => l.TotalDebit - l.TotalCredit);

        IReadOnlyList<ExpenseStatusTotal> statusTotals = await expenses.GetStatusTotalsForPeriodAsync(
            tenantId, query.FromDate, query.ToDate, cancellationToken);

        decimal sourceRecordTotal = statusTotals
            .Where(s => s.Status is ExpenseStatus.Posted or ExpenseStatus.Paid)
            .Sum(s => s.TotalAmount);

        List<ExpenseStatusBreakdownDto> byStatus = statusTotals
            .Select(s => new ExpenseStatusBreakdownDto(s.Status.ToString(), s.Count, s.TotalAmount))
            .OrderBy(s => s.Status)
            .ToList();

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.FromDate, query.ToDate, "Tenant (all expenses)", clock.UtcNow, currentUser.UserId);

        return new ExpenseSummaryReportDto(metadata, ledgerTotal, sourceRecordTotal, ledgerTotal == sourceRecordTotal, byStatus);
    }
}
