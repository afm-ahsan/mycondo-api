using Mediator;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorMaintenanceSchedules;

public sealed record GetGeneratorMaintenanceSchedulesQuery(
    Guid? GeneratorId,
    int Page,
    int PageSize
) : IRequest<PagedResult<GeneratorMaintenanceScheduleDto>>;
