using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Property.Buildings.DTOs;

namespace MyCondo.Application.Features.Property.Buildings.Commands.UpdateBuilding;

public sealed record UpdateBuildingCommand(
    Guid BuildingId,
    string Name,
    string Code,
    string? Address
) : IRequest<BuildingDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "property.buildings";
}
