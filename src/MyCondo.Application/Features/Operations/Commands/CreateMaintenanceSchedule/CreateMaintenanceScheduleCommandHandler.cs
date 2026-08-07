using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Commands.CreateMaintenanceSchedule;

public sealed class CreateMaintenanceScheduleCommandHandler(
    IGeneratorMaintenanceScheduleRepository schedules,
    IGeneratorRepository generators,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateMaintenanceScheduleCommandHandler> logger
) : IRequestHandler<CreateMaintenanceScheduleCommand, GeneratorMaintenanceScheduleDto>
{
    public async ValueTask<GeneratorMaintenanceScheduleDto> Handle(CreateMaintenanceScheduleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorId generatorId = new(command.GeneratorId);
        Generator generator = await generators.GetByIdAsync(generatorId, cancellationToken)
            ?? throw new NotFoundException(nameof(Generator), command.GeneratorId);
        if (generator.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Generator), command.GeneratorId);
        }

        GeneratorMaintenanceSchedule schedule = GeneratorMaintenanceSchedule.Create(
            tenantId, generatorId, command.NextDueDate, command.NextDueHourMeterReading, clock.UtcNow);

        schedules.Add(schedule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Maintenance schedule {GeneratorMaintenanceScheduleId} created for generator {GeneratorId}, tenant {TenantId}",
            schedule.Id, generatorId, tenantId);

        return schedule.ToDto();
    }
}
