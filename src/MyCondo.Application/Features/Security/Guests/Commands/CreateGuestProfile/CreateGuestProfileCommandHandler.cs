using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Guests.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.Guests;

namespace MyCondo.Application.Features.Security.Guests.Commands.CreateGuestProfile;

public sealed class CreateGuestProfileCommandHandler(
    IGuestProfileRepository guestProfiles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateGuestProfileCommandHandler> logger
) : IRequestHandler<CreateGuestProfileCommand, GuestProfileDto>
{
    public async ValueTask<GuestProfileDto> Handle(CreateGuestProfileCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        string phone = command.Phone.Trim();
        GuestProfile? existing = await guestProfiles.GetByPhoneAsync(tenantId, phone, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"A guest profile with phone '{phone}' already exists for this tenant.");
        }

        GuestProfile guest = GuestProfile.Register(
            tenantId, command.FullName, phone, command.IdentityDocumentType, command.IdentityDocumentNumber,
            clock.UtcNow);

        guestProfiles.Add(guest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Guest profile {GuestProfileId} '{FullName}' created for tenant {TenantId}",
            guest.Id, guest.FullName, tenantId);

        return new GuestProfileDto(
            guest.Id.Value, guest.FullName, guest.Phone, guest.IdentityDocumentType,
            IdentityMasking.Mask(guest.IdentityDocumentNumber), guest.IsBlocked, guest.BlockReason);
    }
}
