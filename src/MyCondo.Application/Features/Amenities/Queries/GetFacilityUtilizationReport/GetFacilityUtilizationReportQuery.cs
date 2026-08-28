using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.GetFacilityUtilizationReport;

public sealed record GetFacilityUtilizationReportQuery(
    Guid? FacilityId,
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<IReadOnlyList<FacilityUtilizationReportLineDto>>, IRequiresFeature
{
    public string FeatureKey => "facilities.reports";
}
