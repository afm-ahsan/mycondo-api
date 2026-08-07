using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Features.Operations.GeneratorSessions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Queries.GetOpenGeneratorSession;

public sealed class GetOpenGeneratorSessionQueryHandler(
    IGeneratorSessionRepository sessions,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetOpenGeneratorSessionQuery, GeneratorSessionDto?>
{
    public async ValueTask<GeneratorSessionDto?> Handle(GetOpenGeneratorSessionQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorSession? session = await sessions.GetOpenForGeneratorAsync(
            tenantId, new GeneratorId(query.GeneratorId), cancellationToken);

        return session?.ToDto();
    }
}
