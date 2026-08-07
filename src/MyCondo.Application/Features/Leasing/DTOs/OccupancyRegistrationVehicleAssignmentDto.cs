namespace MyCondo.Application.Features.Leasing.DTOs;

public sealed record OccupancyRegistrationVehicleAssignmentDto(
    Guid OccupancyRegistrationVehicleAssignmentId,
    Guid OccupancyRegistrationId,
    Guid VehicleId,
    string RegistrationNumber,
    string VehicleType,
    bool IsBlocked,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? EndedAtUtc,
    bool IsActive);
