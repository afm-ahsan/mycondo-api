using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseTrendReport;

public sealed record ExpenseTrendMonthDto(int Year, int Month, decimal TotalAmount);

public sealed record ExpenseTrendReportDto(
    FinanceReportMetadataDto Metadata, IReadOnlyList<ExpenseTrendMonthDto> Months, decimal Total);

/// <summary>Ledger-authoritative (OperatingExpense account) expense totals bucketed by calendar month
/// across [FromDate, ToDate] — a time series for a trend chart. Unlike the Category/Type breakdowns,
/// Trend needs no per-Expense dimension, so it is computed purely from ledger activity (no join back to
/// the source Expense aggregate) via <c>IFinanceReportRepository.GetAccountMonthlyActivityAsync</c>, with
/// the month-bucketing itself pushed to SQL.</summary>
public sealed record GetExpenseTrendReportQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<ExpenseTrendReportDto>;
