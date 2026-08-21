using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseSummaryReport;

public sealed record ExpenseStatusBreakdownDto(string Status, int Count, decimal TotalAmount);

/// <summary><see cref="LedgerTotal"/> is the authoritative figure — net debit activity on the tenant's
/// OperatingExpense ledger account for the period. <see cref="SourceRecordTotal"/> is a current-snapshot
/// cross-check (sum of <c>Expense.Amount</c> for expenses whose *current* Status is Posted/Paid) and can
/// legitimately diverge from <see cref="LedgerTotal"/> when an expense originally posted in this period
/// was later voided in a *different* period — the reversal lands in the period it was actually posted,
/// same real double-entry behavior every ledger-authoritative report in this system follows, so that's
/// not a bug, it's exactly the signal <see cref="IsReconciled"/> exists to surface.</summary>
public sealed record ExpenseSummaryReportDto(
    FinanceReportMetadataDto Metadata,
    decimal LedgerTotal,
    decimal SourceRecordTotal,
    bool IsReconciled,
    IReadOnlyList<ExpenseStatusBreakdownDto> ByStatus);

public sealed record GetExpenseSummaryReportQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<ExpenseSummaryReportDto>;
