using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.Guests;

namespace MyCondo.Application.Features.Security.Guests.Commands.BlockGuestProfile;

public sealed class BlockGuestProfileCommandHandler(
    IGuestProfileRepository guestProfiles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<BlockGuestProfileCommandHandler> logger
) : IRequestHandler<BlockGuestProfileCommand>
{
    public async ValueTask<Unit> Handle(BlockGuestProfileCommand command, CancellationToken cancellationToken)
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

        guest.Block(command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Guest profile {GuestProfileId} blocked for tenant {TenantId}: {Reason}",
            id, tenantId, command.Reason);

        return Unit.Value;
    }
}
