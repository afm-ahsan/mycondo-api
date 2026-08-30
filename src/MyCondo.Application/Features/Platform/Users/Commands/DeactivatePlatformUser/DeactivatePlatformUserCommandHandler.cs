using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Features.Platform.Users.Commands.DeactivatePlatformUser;

public sealed class DeactivatePlatformUserCommandHandler(
    IPlatformUserRepository platformUsers,
    IUnitOfWork unitOfWork,
    ICurrentPlatformUserProvider currentUser,
    IPlatformSuperAdminProtectionService superAdminProtection,
    IPlatformAuditLogRepository platformAuditLog,
    IClock clock,
    ILogger<DeactivatePlatformUserCommandHandler> logger
) : IRequestHandler<DeactivatePlatformUserCommand>
{
    public async ValueTask<Unit> Handle(DeactivatePlatformUserCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PlatformUserId platformUserId = new(command.PlatformUserId);
        PlatformUser user = await platformUsers.GetByIdAsync(platformUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlatformUser), command.PlatformUserId);

        // mycondo-docs ADR-035 — a Super Admin cannot self-disable, a lower-privileged actor cannot
        // disable a Super Admin, and the platform's last active Super Admin can never be disabled.
        bool targetIsSuperAdmin = await superAdminProtection.TargetIsSuperAdminAsync(platformUserId, cancellationToken);

        if (targetIsSuperAdmin)
        {
            superAdminProtection.EnsureCanMutateAdminTarget(
                platformUserId.Value, currentUser.PlatformUserId ?? Guid.Empty,
                currentUser.HasPermission("platform.user.manageSuperAdmins"));
        }

        await using IUnitOfWorkTransaction? transaction = targetIsSuperAdmin
            ? await unitOfWork.BeginTransactionAsync(cancellationToken)
            : null;

        if (targetIsSuperAdmin)
        {
            await superAdminProtection.EnsureNotLastActiveSuperAdminAsync(cancellationToken);
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        user.Deactivate(nowUtc);
        platformAuditLog.Add(PlatformAuditLogEntry.Record(
            nowUtc, currentUser.PlatformUserId, "PlatformUser.Deactivate", nameof(PlatformUser), platformUserId.Value.ToString()));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        logger.LogInformation("Platform user {PlatformUserId} deactivated", platformUserId);

        return Unit.Value;
    }
}
