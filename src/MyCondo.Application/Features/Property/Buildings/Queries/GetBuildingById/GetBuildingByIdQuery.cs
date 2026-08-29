using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Buildings.DTOs;

namespace MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingById;

public sealed record GetBuildingByIdQuery(Guid BuildingId) : IRequest<BuildingDto>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "property.buildings";
}
