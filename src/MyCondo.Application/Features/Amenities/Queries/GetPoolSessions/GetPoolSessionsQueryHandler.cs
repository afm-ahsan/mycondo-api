using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolSessions;

public sealed class GetPoolSessionsQueryHandler(
    IPoolSessionRepository poolSessions,
    IFlatDisplayNameResolver flatDisplayNames,
    IUserRepository users,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetPoolSessionsQuery, PagedResult<PoolSessionDto>>
{
    public async ValueTask<PagedResult<PoolSessionDto>> Handle(GetPoolSessionsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId? facilityId = query.FacilityId is Guid rawFacilityId ? new FacilityId(rawFacilityId) : null;
        FlatId? flatId = query.FlatId is Guid rawFlatId ? new FlatId(rawFlatId) : null;

        PagedResult<PoolSession> result = await poolSessions.SearchAsync(
            tenantId, facilityId, flatId, query.OpenOnly, query.Page, query.PageSize, cancellationToken);

        IReadOnlyDictionary<FlatId, string> flatNamesById = await flatDisplayNames.ResolveManyAsync(
            result.Items.Select(p => p.FlatId).Distinct().ToList(), cancellationToken);

        List<UserId> actorIds = result.Items
            .SelectMany(p => new[] { p.CheckedInBy, p.CheckedOutBy })
            .Where(actorId => actorId is not null)
            .Select(actorId => new UserId(actorId!.Value))
            .Distinct()
            .ToList();
        Dictionary<Guid, string> actorNamesById = (await users.GetByIdsAsync(tenantId, actorIds, cancellationToken))
            .ToDictionary(u => u.Id.Value, u => u.FullName);

        List<PoolSessionDto> items = result.Items
            .Select(p => p.ToDto(
                flatNamesById.GetValueOrDefault(p.FlatId, "Unknown flat"),
                p.CheckedInBy is null ? "System" : actorNamesById.GetValueOrDefault(p.CheckedInBy.Value, "Unknown user"),
                p.ExitAtUtc is null
                    ? null
                    : p.CheckedOutBy is null
                        ? "System" : actorNamesById.GetValueOrDefault(p.CheckedOutBy.Value, "Unknown user")))
            .ToList();

        return new PagedResult<PoolSessionDto>(items, result.Page, result.PageSize, result.Total);
    }
}
