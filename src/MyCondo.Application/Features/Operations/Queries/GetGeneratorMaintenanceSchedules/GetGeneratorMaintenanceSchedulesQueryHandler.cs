using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorMaintenanceSchedules;

public sealed class GetGeneratorMaintenanceSchedulesQueryHandler(
    IGeneratorMaintenanceScheduleRepository schedules,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGeneratorMaintenanceSchedulesQuery, PagedResult<GeneratorMaintenanceScheduleDto>>
{
    public async ValueTask<PagedResult<GeneratorMaintenanceScheduleDto>> Handle(
        GetGeneratorMaintenanceSchedulesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorId? generatorId = query.GeneratorId is Guid raw ? new GeneratorId(raw) : null;

        PagedResult<GeneratorMaintenanceSchedule> result = await schedules.SearchAsync(
            tenantId, generatorId, query.Page, query.PageSize, cancellationToken);

        List<GeneratorMaintenanceScheduleDto> items = result.Items.Select(x => x.ToDto()).ToList();

        return new PagedResult<GeneratorMaintenanceScheduleDto>(items, result.Page, result.PageSize, result.Total);
    }
}
