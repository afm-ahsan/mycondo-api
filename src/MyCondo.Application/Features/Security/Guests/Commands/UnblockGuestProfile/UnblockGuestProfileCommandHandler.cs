using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.Guests;

namespace MyCondo.Application.Features.Security.Guests.Commands.UnblockGuestProfile;

public sealed class UnblockGuestProfileCommandHandler(
    IGuestProfileRepository guestProfiles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<UnblockGuestProfileCommandHandler> logger
) : IRequestHandler<UnblockGuestProfileCommand>
{
    public async ValueTask<Unit> Handle(UnblockGuestProfileCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GuestProfileId id = new(command.GuestProfileId);
        GuestProfile guest = await guestProfiles.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(GuestProfile), command.GuestProfileId);

        if (guest.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(GuestProfile), command.GuestProfileId);
        }

        guest.Unblock();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Guest profile {GuestProfileId} unblocked for tenant {TenantId}", id, tenantId);

        return Unit.Value;
    }
}
