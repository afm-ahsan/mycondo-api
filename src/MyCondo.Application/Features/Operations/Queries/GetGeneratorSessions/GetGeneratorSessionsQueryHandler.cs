using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GeneratorSessions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorSessions;

public sealed class GetGeneratorSessionsQueryHandler(
    IGeneratorSessionRepository sessions,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGeneratorSessionsQuery, PagedResult<GeneratorSessionDto>>
{
    public async ValueTask<PagedResult<GeneratorSessionDto>> Handle(GetGeneratorSessionsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorId? generatorId = query.GeneratorId is Guid rawGeneratorId ? new GeneratorId(rawGeneratorId) : null;
        GeneratorSessionStatus? status = query.Status is null ? null : Enum.Parse<GeneratorSessionStatus>(query.Status);

        PagedResult<GeneratorSession> result = await sessions.SearchAsync(
            tenantId, generatorId, status, query.Page, query.PageSize, cancellationToken);

        List<GeneratorSessionDto> items = result.Items.Select(s => s.ToDto()).ToList();

        return new PagedResult<GeneratorSessionDto>(items, result.Page, result.PageSize, result.Total);
    }
}
