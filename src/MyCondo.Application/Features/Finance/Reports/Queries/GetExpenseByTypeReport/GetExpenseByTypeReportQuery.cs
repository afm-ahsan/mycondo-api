using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByTypeReport;

public sealed record ExpenseByTypeLineDto(
    Guid ExpenseTypeId, string TypeName, Guid? ExpenseCategoryId, string CategoryName, int Count, decimal TotalAmount);

public sealed record ExpenseByTypeReportDto(
    FinanceReportMetadataDto Metadata, IReadOnlyList<ExpenseByTypeLineDto> Lines, decimal Total);

/// <summary>Posted (Posted/Paid) expense totals grouped by Expense Type for a period — one level below
/// <c>GetExpenseByCategoryReportQuery</c>, same posted-only population
/// (<c>IExpenseRepository.GetExpenseCompositionByTypeAsync</c>).</summary>
public sealed record GetExpenseByTypeReportQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<ExpenseByTypeReportDto>;
