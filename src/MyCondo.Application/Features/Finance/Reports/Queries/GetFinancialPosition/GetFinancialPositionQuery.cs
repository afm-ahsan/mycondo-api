using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialPosition;

public sealed record FinancialPositionLineDto(Guid ChartOfAccountId, string Code, string Name, decimal Balance);

public sealed record FinancialPositionSectionDto(
    IReadOnlyList<FinancialPositionLineDto> Lines,
    decimal Total);

public sealed record FinancialPositionReportDto(
    FinanceReportMetadataDto Metadata,
    FinancialPositionSectionDto Assets,
    FinancialPositionSectionDto Liabilities,
    FinancialPositionSectionDto Equity,
    decimal RetainedSurplusDeficit,
    decimal TotalLiabilitiesAndEquity);

public sealed record GetFinancialPositionQuery(DateOnly? AsOfDate) : IRequest<FinancialPositionReportDto>;
