namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record GeneratorDto(
    Guid GeneratorId,
    Guid BuildingId,
    string Name,
    string? Model,
    decimal? CapacityKva,
    string? Location,
    decimal CurrentHourMeterReading,
    bool IsActive);
