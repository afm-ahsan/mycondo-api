using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Features.Amenities.PoolSessions;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolSessionById;

public sealed class GetPoolSessionByIdQueryHandler(
    IPoolSessionRepository poolSessions,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetPoolSessionByIdQuery, PoolSessionDto>
{
    public async ValueTask<PoolSessionDto> Handle(GetPoolSessionByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PoolSessionId id = new(query.PoolSessionId);
        PoolSession session = await poolSessions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(PoolSession), query.PoolSessionId);
        if (session.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(PoolSession), query.PoolSessionId);
        }

        return session.ToDto();
    }
}
