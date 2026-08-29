using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorMaintenanceSchedules;

public sealed record GetGeneratorMaintenanceSchedulesQuery(
    Guid? GeneratorId,
    int Page,
    int PageSize
) : IRequest<PagedResult<GeneratorMaintenanceScheduleDto>>, IRequiresFeature, ILifecycleReadOperation
{
    public string FeatureKey => "operations.generator";
}
