using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.AccessSessions.DTOs;
using MyCondo.Application.Features.Security.AccessSessions.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.Guests;

namespace MyCondo.Application.Features.Security.AccessSessions.Queries.GetAccessSessionsForGuestProfile;

public sealed class GetAccessSessionsForGuestProfileQueryHandler(
    IAccessSessionRepository accessSessions,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAccessSessionsForGuestProfileQuery, PagedResult<AccessSessionDto>>
{
    public async ValueTask<PagedResult<AccessSessionDto>> Handle(GetAccessSessionsForGuestProfileQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<AccessSession> result = await accessSessions.SearchForGuestProfileAsync(
            tenantId, new GuestProfileId(query.GuestProfileId), query.Page, query.PageSize, cancellationToken);

        List<AccessSessionDto> items = result.Items.Select(s => s.ToDto()).ToList();

        return new PagedResult<AccessSessionDto>(items, result.Page, result.PageSize, result.Total);
    }
}
