using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;

namespace MyCondo.Application.Features.Operations.Commands.UpdateMaintenanceSchedule;

public sealed class UpdateMaintenanceScheduleCommandHandler(
    IGeneratorMaintenanceScheduleRepository schedules,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<UpdateMaintenanceScheduleCommandHandler> logger
) : IRequestHandler<UpdateMaintenanceScheduleCommand, GeneratorMaintenanceScheduleDto>
{
    public async ValueTask<GeneratorMaintenanceScheduleDto> Handle(UpdateMaintenanceScheduleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorMaintenanceScheduleId id = new(command.GeneratorMaintenanceScheduleId);
        GeneratorMaintenanceSchedule schedule = await schedules.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(GeneratorMaintenanceSchedule), command.GeneratorMaintenanceScheduleId);
        if (schedule.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(GeneratorMaintenanceSchedule), command.GeneratorMaintenanceScheduleId);
        }

        schedule.Reschedule(command.NextDueDate, command.NextDueHourMeterReading);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Maintenance schedule {GeneratorMaintenanceScheduleId} updated, tenant {TenantId}", id, tenantId);

        return schedule.ToDto();
    }
}
