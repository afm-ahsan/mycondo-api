using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetTrialBalance;

public sealed record TrialBalanceLineDto(
    Guid ChartOfAccountId,
    string Code,
    string Name,
    string Category,
    string NormalBalance,
    decimal Debit,
    decimal Credit);

public sealed record TrialBalanceReportDto(
    FinanceReportMetadataDto Metadata,
    IReadOnlyList<TrialBalanceLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit);

public sealed record GetTrialBalanceQuery(DateOnly? AsOfDate) : IRequest<TrialBalanceReportDto>;
