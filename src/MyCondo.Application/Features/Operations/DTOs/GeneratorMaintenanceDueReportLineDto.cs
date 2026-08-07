namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record GeneratorMaintenanceDueReportLineDto(
    Guid GeneratorId,
    string GeneratorName,
    Guid GeneratorMaintenanceScheduleId,
    DateOnly? NextDueDate,
    decimal? NextDueHourMeterReading,
    decimal CurrentHourMeterReading,
    bool IsDue);
