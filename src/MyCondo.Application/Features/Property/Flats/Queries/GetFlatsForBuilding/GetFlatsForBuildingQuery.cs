using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Property.Flats.Queries.GetFlatsForBuilding;

public sealed record GetFlatsForBuildingQuery(
    Guid BuildingId,
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<FlatDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "property.flats";
}
