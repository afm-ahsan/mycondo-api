using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;

public sealed record CreateBuildingCommand(
    string Name,
    string Code,
    string? Address
) : IRequest<CreateBuildingResult>, IRequiresFeature
{
    public string FeatureKey => "property.buildings";
}
