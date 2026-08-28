using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorSessions;

public sealed record GetGeneratorSessionsQuery(
    Guid? GeneratorId,
    string? Status,
    int Page,
    int PageSize
) : IRequest<PagedResult<GeneratorSessionDto>>, IRequiresFeature
{
    public string FeatureKey => "operations.generator";
}
