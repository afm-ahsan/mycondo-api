using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Reports;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetIncomeExpenseReport;

/// <summary>Period Income, Expense, and Surplus/Deficit (Income - Expense — never cash-derived, per
/// the Template 5 "permanently distinct figures" rule) for a [FromDate, ToDate] window. Ledger-derived
/// via <c>IFinanceReportRepository.GetCategoryActivityAsync</c>, a period-scoped sibling of
/// <c>GetTrialBalanceAsync</c> — not a re-derivation from Invoice/Expense entities.</summary>
public sealed class GetIncomeExpenseReportQueryHandler(
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetIncomeExpenseReportQuery, IncomeExpenseReportDto>
{
    public async ValueTask<IncomeExpenseReportDto> Handle(GetIncomeExpenseReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<TrialBalanceAccountLine> incomeActivity = await reports.GetCategoryActivityAsync(
            tenantId, AccountCategory.Income, query.FromDate, query.ToDate, cancellationToken);
        IReadOnlyList<TrialBalanceAccountLine> expenseActivity = await reports.GetCategoryActivityAsync(
            tenantId, AccountCategory.Expense, query.FromDate, query.ToDate, cancellationToken);

        List<IncomeExpenseLineDto> incomeLines = incomeActivity
            .Select(l => new IncomeExpenseLineDto(l.ChartOfAccountId.Value, l.Code, l.Name, l.TotalCredit - l.TotalDebit))
            .Where(l => l.Amount != 0m)
            .OrderBy(l => l.Code)
            .ToList();

        List<IncomeExpenseLineDto> expenseLines = expenseActivity
            .Select(l => new IncomeExpenseLineDto(l.ChartOfAccountId.Value, l.Code, l.Name, l.TotalDebit - l.TotalCredit))
            .Where(l => l.Amount != 0m)
            .OrderBy(l => l.Code)
            .ToList();

        decimal totalIncome = incomeLines.Sum(l => l.Amount);
        decimal totalExpense = expenseLines.Sum(l => l.Amount);

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.FromDate, query.ToDate, "Tenant (all accounts)", clock.UtcNow, currentUser.UserId);

        return new IncomeExpenseReportDto(
            metadata, incomeLines, totalIncome, expenseLines, totalExpense, totalIncome - totalExpense);
    }
}
