using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.CompleteMaintenanceService;

public sealed record CompleteMaintenanceServiceCommand(
    Guid GeneratorMaintenanceScheduleId,
    DateTimeOffset PerformedAtUtc,
    string Description,
    decimal? Cost,
    DateOnly? NextDueDate,
    decimal? NextDueHourMeterReading
) : IRequest<GeneratorServiceRecordDto>;
