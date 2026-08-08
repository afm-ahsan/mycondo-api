using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Queries.GetMeterStatusSummaryReport;

public sealed record GetMeterStatusSummaryReportQuery(
    Guid? BuildingId,
    string? UtilityType
) : IRequest<IReadOnlyList<MeterStatusSummaryLineDto>>;
