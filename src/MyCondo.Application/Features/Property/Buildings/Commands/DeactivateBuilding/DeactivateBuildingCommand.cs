using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Property.Buildings.Commands.DeactivateBuilding;

public sealed record DeactivateBuildingCommand(Guid BuildingId) : IRequest, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "property.buildings";
}
