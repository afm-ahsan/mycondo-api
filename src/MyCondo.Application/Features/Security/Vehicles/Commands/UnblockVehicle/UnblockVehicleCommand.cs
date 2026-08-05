using Mediator;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.UnblockVehicle;

public sealed record UnblockVehicleCommand(Guid VehicleId) : IRequest;
