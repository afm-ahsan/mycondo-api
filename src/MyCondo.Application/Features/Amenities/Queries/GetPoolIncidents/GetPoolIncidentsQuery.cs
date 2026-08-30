using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolIncidents;

public sealed record GetPoolIncidentsQuery(
    Guid? FacilityId,
    int Page,
    int PageSize
) : IRequest<PagedResult<PoolIncidentDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "facilities.swimming_pool";
}
