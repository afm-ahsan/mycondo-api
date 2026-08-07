using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.UpdateMaintenanceSchedule;

public sealed record UpdateMaintenanceScheduleCommand(
    Guid GeneratorMaintenanceScheduleId,
    DateOnly? NextDueDate,
    decimal? NextDueHourMeterReading
) : IRequest<GeneratorMaintenanceScheduleDto>;
