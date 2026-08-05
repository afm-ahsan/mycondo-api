using Mediator;
using MyCondo.Application.Features.Security.Vehicles.DTOs;

namespace MyCondo.Application.Features.Security.Vehicles.Commands.RegisterVehicle;

public sealed record RegisterVehicleCommand(
    string RegistrationNumber,
    string VehicleType,
    string? Make,
    string? Model,
    string? Color,
    string OwnershipCategory,
    Guid? FlatId
) : IRequest<VehicleDto>;
