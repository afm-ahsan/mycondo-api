using Mediator;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Guests.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.Guests;

namespace MyCondo.Application.Features.Security.Guests.Queries.GetGuestProfilesForTenant;

public sealed class GetGuestProfilesForTenantQueryHandler(
    IGuestProfileRepository guestProfiles,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGuestProfilesForTenantQuery, PagedResult<GuestProfileDto>>
{
    public async ValueTask<PagedResult<GuestProfileDto>> Handle(GetGuestProfilesForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<GuestProfile> result = await guestProfiles.SearchAsync(
            tenantId, query.Search, query.Page, query.PageSize, cancellationToken);

        List<GuestProfileDto> items = result.Items
            .Select(g => new GuestProfileDto(
                g.Id.Value, g.FullName, g.Phone, g.IdentityDocumentType,
                IdentityMasking.Mask(g.IdentityDocumentNumber), g.IsBlocked, g.BlockReason))
            .ToList();

        return new PagedResult<GuestProfileDto>(items, result.Page, result.PageSize, result.Total);
    }
}
