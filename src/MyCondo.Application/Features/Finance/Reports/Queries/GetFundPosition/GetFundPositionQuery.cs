using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFundPosition;

public sealed record FundPositionLineDto(Guid FundId, string Code, string Name, decimal Balance);

public sealed record FundPositionReportDto(
    FinanceReportMetadataDto Metadata,
    IReadOnlyList<FundPositionLineDto> Funds,
    decimal TotalBalance);

public sealed record GetFundPositionQuery(DateOnly? AsOfDate) : IRequest<FundPositionReportDto>;
