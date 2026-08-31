using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Features.Platform.Users.Commands.ActivatePlatformUser;

public sealed class ActivatePlatformUserCommandHandler(
    IPlatformUserRepository platformUsers,
    IUnitOfWork unitOfWork,
    ICurrentPlatformUserProvider currentUser,
    IPlatformSuperAdminProtectionService superAdminProtection,
    IPlatformAuditLogRepository platformAuditLog,
    IClock clock,
    ILogger<ActivatePlatformUserCommandHandler> logger
) : IRequestHandler<ActivatePlatformUserCommand>
{
    public async ValueTask<Unit> Handle(ActivatePlatformUserCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PlatformUserId platformUserId = new(command.PlatformUserId);
        PlatformUser user = await platformUsers.GetByIdAsync(platformUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlatformUser), command.PlatformUserId);

        // mycondo-docs ADR-036 — re-activating never reduces the platform's active-Super-Admin count,
        // so no last-admin check here, only the target-privilege composition (self always allowed).
        if (await superAdminProtection.TargetIsSuperAdminAsync(platformUserId, cancellationToken))
        {
            superAdminProtection.EnsureCanEditAdminTarget(
                platformUserId.Value, currentUser.PlatformUserId ?? Guid.Empty,
                currentUser.HasPermission("platform.user.manageSuperAdmins"));
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        user.Activate(nowUtc);
        platformAuditLog.Add(PlatformAuditLogEntry.Record(
            nowUtc, currentUser.PlatformUserId, "PlatformUser.Activate", nameof(PlatformUser), platformUserId.Value.ToString()));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Platform user {PlatformUserId} activated", platformUserId);

        return Unit.Value;
    }
}
