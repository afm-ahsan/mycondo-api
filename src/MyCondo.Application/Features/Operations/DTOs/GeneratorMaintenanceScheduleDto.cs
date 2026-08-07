namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record GeneratorMaintenanceScheduleDto(
    Guid GeneratorMaintenanceScheduleId,
    Guid GeneratorId,
    DateOnly? NextDueDate,
    decimal? NextDueHourMeterReading,
    bool IsActive);
