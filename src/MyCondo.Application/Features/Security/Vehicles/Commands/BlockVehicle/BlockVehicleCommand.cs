using Mediator;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.BlockVehicle;

public sealed record BlockVehicleCommand(Guid VehicleId, string Reason) : IRequest;
