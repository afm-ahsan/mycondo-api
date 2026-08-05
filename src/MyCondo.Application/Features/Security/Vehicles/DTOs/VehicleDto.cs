namespace MyCondo.Application.Features.Security.Vehicles.DTOs;

public sealed record VehicleDto(
    Guid VehicleId,
    string RegistrationNumber,
    string VehicleType,
    string? Make,
    string? Model,
    string? Color,
    string OwnershipCategory,
    Guid? FlatId,
    bool IsBlocked,
    string? BlockReason);
