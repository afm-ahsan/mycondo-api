using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetIncomeExpenseReport;

public sealed record IncomeExpenseLineDto(Guid ChartOfAccountId, string Code, string Name, decimal Amount);

public sealed record IncomeExpenseReportDto(
    FinanceReportMetadataDto Metadata,
    IReadOnlyList<IncomeExpenseLineDto> IncomeLines,
    decimal TotalIncome,
    IReadOnlyList<IncomeExpenseLineDto> ExpenseLines,
    decimal TotalExpense,
    decimal SurplusDeficit);

public sealed record GetIncomeExpenseReportQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<IncomeExpenseReportDto>;
