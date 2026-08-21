using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Reports;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseTrendReport;

public sealed class GetExpenseTrendReportQueryHandler(
    IFinanceReportRepository reports,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetExpenseTrendReportQuery, ExpenseTrendReportDto>
{
    public async ValueTask<ExpenseTrendReportDto> Handle(GetExpenseTrendReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForPeriod(
            query.FromDate, query.ToDate, "Tenant (all expenses)", clock.UtcNow, currentUser.UserId);

        Guid? operatingExpenseAccountId = await reports.GetChartOfAccountIdForPostingRoleAsync(
            tenantId, nameof(LedgerAccountType.OperatingExpense), cancellationToken);

        if (operatingExpenseAccountId is not Guid accountId)
        {
            // No mapping yet ("no default account" posture, same as IFinancialPostingService) — no
            // expenses could have been posted, so the trend is genuinely empty rather than an error.
            return new ExpenseTrendReportDto(metadata, [], 0m);
        }

        IReadOnlyList<MonthlyAccountActivityLine> monthly = await reports.GetAccountMonthlyActivityAsync(
            tenantId, accountId, query.FromDate, query.ToDate, cancellationToken);

        List<ExpenseTrendMonthDto> months = monthly
            .Select(m => new ExpenseTrendMonthDto(m.Year, m.Month, m.TotalDebit - m.TotalCredit))
            .ToList();

        return new ExpenseTrendReportDto(metadata, months, months.Sum(m => m.TotalAmount));
    }
}
