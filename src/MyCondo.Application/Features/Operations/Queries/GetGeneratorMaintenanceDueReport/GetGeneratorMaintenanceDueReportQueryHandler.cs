using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorMaintenanceDueReport;

public sealed class GetGeneratorMaintenanceDueReportQueryHandler(
    IGeneratorMaintenanceScheduleRepository schedules,
    IGeneratorRepository generators,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetGeneratorMaintenanceDueReportQuery, IReadOnlyList<GeneratorMaintenanceDueReportLineDto>>
{
    public async ValueTask<IReadOnlyList<GeneratorMaintenanceDueReportLineDto>> Handle(
        GetGeneratorMaintenanceDueReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        IReadOnlyList<GeneratorMaintenanceSchedule> activeSchedules = await schedules.ListActiveAsync(tenantId, cancellationToken);

        List<GeneratorMaintenanceDueReportLineDto> lines = [];
        foreach (GeneratorMaintenanceSchedule schedule in activeSchedules)
        {
            Generator? generator = await generators.GetByIdAsync(schedule.GeneratorId, cancellationToken);
            decimal currentReading = generator?.CurrentHourMeterReading ?? 0m;

            lines.Add(new GeneratorMaintenanceDueReportLineDto(
                schedule.GeneratorId.Value, generator?.Name ?? "(unknown)", schedule.Id.Value, schedule.NextDueDate,
                schedule.NextDueHourMeterReading, currentReading, schedule.IsDue(today, currentReading)));
        }

        return lines;
    }
}
