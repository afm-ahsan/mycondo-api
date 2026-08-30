using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.CreateMaintenanceSchedule;

public sealed record CreateMaintenanceScheduleCommand(
    Guid GeneratorId,
    DateOnly? NextDueDate,
    decimal? NextDueHourMeterReading
) : IRequest<GeneratorMaintenanceScheduleDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "operations.generator";
}
