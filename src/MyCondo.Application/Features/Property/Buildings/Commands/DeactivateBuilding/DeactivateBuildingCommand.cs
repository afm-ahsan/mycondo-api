using Mediator;

namespace MyCondo.Application.Features.Property.Buildings.Commands.DeactivateBuilding;

public sealed record DeactivateBuildingCommand(Guid BuildingId) : IRequest;
