using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetGenerators;

public sealed record GetGeneratorsQuery(
    Guid? BuildingId,
    int Page,
    int PageSize
) : IRequest<PagedResult<GeneratorDto>>, IRequiresFeature
{
    public string FeatureKey => "operations.generator";
}
