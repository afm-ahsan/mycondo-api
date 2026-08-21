using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByCategoryReport;

public sealed record ExpenseByCategoryLineDto(Guid? ExpenseCategoryId, string CategoryName, decimal TotalAmount);

public sealed record ExpenseByCategoryReportDto(
    FinanceReportMetadataDto Metadata, IReadOnlyList<ExpenseByCategoryLineDto> Lines, decimal Total);

/// <summary>Posted (Posted/Paid) expense totals grouped by Expense Category for a period. The tenant-wide
/// OperatingExpense ledger account carries no category dimension of its own (Template 3), so
/// <c>ExpenseCategory</c> is joined on as the operational dimension per source Expense — see
/// <c>IExpenseRepository.GetExpenseCompositionByCategoryAsync</c>, the same method the Financial
/// Overview report uses, so this report and that one can never disagree on category composition.
/// </summary>
public sealed record GetExpenseByCategoryReportQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<ExpenseByCategoryReportDto>;
