using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;
using MyCondo.Domain.Features.Operations.GeneratorServiceRecords;

namespace MyCondo.Application.Features.Operations.Commands.CompleteMaintenanceService;

/// <summary>Records the completed service AND advances the schedule's next-due point in the same
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call — "Complete maintenance action" (register-
/// digitization spec §5.13) is one user-facing action, not two separate steps.</summary>
public sealed class CompleteMaintenanceServiceCommandHandler(
    IGeneratorServiceRecordRepository serviceRecords,
    IGeneratorMaintenanceScheduleRepository schedules,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CompleteMaintenanceServiceCommandHandler> logger
) : IRequestHandler<CompleteMaintenanceServiceCommand, GeneratorServiceRecordDto>
{
    public async ValueTask<GeneratorServiceRecordDto> Handle(CompleteMaintenanceServiceCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorMaintenanceScheduleId scheduleId = new(command.GeneratorMaintenanceScheduleId);
        GeneratorMaintenanceSchedule schedule = await schedules.GetByIdAsync(scheduleId, cancellationToken)
            ?? throw new NotFoundException(nameof(GeneratorMaintenanceSchedule), command.GeneratorMaintenanceScheduleId);
        if (schedule.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(GeneratorMaintenanceSchedule), command.GeneratorMaintenanceScheduleId);
        }

        GeneratorServiceRecord record = GeneratorServiceRecord.Record(
            tenantId, schedule.GeneratorId, command.PerformedAtUtc, command.Description, command.Cost,
            currentUser.UserId, clock.UtcNow);
        serviceRecords.Add(record);

        schedule.Reschedule(command.NextDueDate, command.NextDueHourMeterReading);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Maintenance service {GeneratorServiceRecordId} completed for generator {GeneratorId}, schedule {GeneratorMaintenanceScheduleId} rescheduled, tenant {TenantId}",
            record.Id, schedule.GeneratorId, scheduleId, tenantId);

        return record.ToDto();
    }
}
