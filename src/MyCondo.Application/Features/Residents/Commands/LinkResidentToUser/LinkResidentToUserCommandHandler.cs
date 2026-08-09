using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Residents.Commands.LinkResidentToUser;

/// <summary>
/// The one, explicit, admin-only way a Resident party record gets bridged to a portal User account
/// (Phase 3, mycondo-docs ADR-021) — never automatic, never inferred from name/email/phone. Both sides
/// must already belong to the caller's own Tenant; no cross-tenant linking is possible.
/// </summary>
public sealed class LinkResidentToUserCommandHandler(
    IResidentRepository residents,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<LinkResidentToUserCommandHandler> logger
) : IRequestHandler<LinkResidentToUserCommand>
{
    public async ValueTask<Unit> Handle(LinkResidentToUserCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ResidentId residentId = new(command.ResidentId);
        Resident resident = await residents.GetByIdAsync(residentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), command.ResidentId);

        if (resident.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Resident), command.ResidentId);
        }

        UserId userId = new(command.UserId);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), command.UserId);

        if (user.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(User), command.UserId);
        }

        resident.LinkToUser(command.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Resident {ResidentId} linked to user {UserId} for tenant {TenantId}", residentId, command.UserId, tenantId);

        return Unit.Value;
    }
}
