using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolSessionById;

public sealed class GetPoolSessionByIdQueryHandler(
    IPoolSessionRepository poolSessions,
    IFlatDisplayNameResolver flatDisplayNames,
    IUserRepository users,
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

        string flatDisplayName = await flatDisplayNames.ResolveAsync(session.FlatId, cancellationToken);

        List<UserId> actorIds = new[] { session.CheckedInBy, session.CheckedOutBy }
            .Where(actorId => actorId is not null)
            .Select(actorId => new UserId(actorId!.Value))
            .Distinct()
            .ToList();
        Dictionary<Guid, string> actorNamesById = (await users.GetByIdsAsync(tenantId, actorIds, cancellationToken))
            .ToDictionary(u => u.Id.Value, u => u.FullName);

        string checkedInByDisplayName = session.CheckedInBy is null
            ? "System" : actorNamesById.GetValueOrDefault(session.CheckedInBy.Value, "Unknown user");
        string? checkedOutByDisplayName = session.ExitAtUtc is null
            ? null
            : session.CheckedOutBy is null
                ? "System" : actorNamesById.GetValueOrDefault(session.CheckedOutBy.Value, "Unknown user");

        return session.ToDto(flatDisplayName, checkedInByDisplayName, checkedOutByDisplayName);
    }
}
