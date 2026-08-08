using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Queries.GetReadingStatusSummaryReport;

public sealed record GetReadingStatusSummaryReportQuery(
    Guid? BuildingId,
    string? UtilityType
) : IRequest<IReadOnlyList<ReadingStatusSummaryLineDto>>;
