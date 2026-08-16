using Mediator;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Guests.DTOs;
using MyCondo.Domain.Common.PhoneNumbers;
using MyCondo.Domain.Features.Security.Guests;

namespace MyCondo.Application.Features.Security.Guests.Queries.GetGuestProfileByPhone;

public sealed class GetGuestProfileByPhoneQueryHandler(
    IGuestProfileRepository guestProfiles,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGuestProfileByPhoneQuery, GuestProfileDto?>
{
    public async ValueTask<GuestProfileDto?> Handle(GetGuestProfileByPhoneQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        if (!BangladeshMobileNumber.TryNormalize(query.Phone, out string? normalizedPhone) || normalizedPhone is null)
        {
            return null;
        }

        GuestProfile? guest = await guestProfiles.GetByPhoneAsync(tenantId, normalizedPhone, cancellationToken);

        return guest is null
            ? null
            : new GuestProfileDto(
                guest.Id.Value, guest.FullName, guest.Phone, guest.IdentityDocumentType,
                IdentityMasking.Mask(guest.IdentityDocumentNumber), guest.IsBlocked, guest.BlockReason);
    }
}
