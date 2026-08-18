using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.ParcelCustodyEvents.DTOs;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Security.ParcelCustodyEvents;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.ParcelCustodyEvents.Queries.GetCustodyHistoryForParcel;

public sealed class GetCustodyHistoryForParcelQueryHandler(
    IParcelCustodyEventRepository custodyEvents,
    IUserRepository users,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetCustodyHistoryForParcelQuery, List<ParcelCustodyEventDto>>
{
    private const string SystemActor = "System";
    private const string UnknownActor = "Unknown user";

    public async ValueTask<List<ParcelCustodyEventDto>> Handle(GetCustodyHistoryForParcelQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<ParcelCustodyEvent> events = await custodyEvents.GetForParcelAsync(
            tenantId, new ParcelId(query.ParcelId), cancellationToken);

        List<UserId> actorIds = events
            .Where(e => e.PerformedBy is not null)
            .Select(e => new UserId(e.PerformedBy!.Value))
            .Distinct()
            .ToList();
        Dictionary<UserId, string> actorNamesById = (await users.GetByIdsAsync(tenantId, actorIds, cancellationToken))
            .ToDictionary(u => u.Id, u => u.FullName);

        return events
            .Select(e => new ParcelCustodyEventDto(
                e.Id.Value, e.ParcelId.Value, e.ToStatus.ToString(), e.OccurredAtUtc, e.PerformedBy,
                e.PerformedBy is null
                    ? SystemActor
                    : actorNamesById.GetValueOrDefault(new UserId(e.PerformedBy.Value), UnknownActor),
                e.Notes))
            .ToList();
    }
}
