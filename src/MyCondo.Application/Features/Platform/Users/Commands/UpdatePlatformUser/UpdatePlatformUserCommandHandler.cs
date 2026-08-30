using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Features.Platform.Users.Commands.UpdatePlatformUser;

public sealed class UpdatePlatformUserCommandHandler(
    IPlatformUserRepository platformUsers,
    IUnitOfWork unitOfWork,
    ICurrentPlatformUserProvider currentUser,
    IPlatformSuperAdminProtectionService superAdminProtection,
    IPlatformAuditLogRepository platformAuditLog,
    IClock clock,
    ILogger<UpdatePlatformUserCommandHandler> logger
) : IRequestHandler<UpdatePlatformUserCommand>
{
    public async ValueTask<Unit> Handle(UpdatePlatformUserCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PlatformUserId platformUserId = new(command.PlatformUserId);
        PlatformUser user = await platformUsers.GetByIdAsync(platformUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlatformUser), command.PlatformUserId);

        // mycondo-docs ADR-035 — a Super Admin may always edit its own profile; a lower-privileged
        // actor may never edit another Super Admin's profile.
        if (await superAdminProtection.TargetIsSuperAdminAsync(platformUserId, cancellationToken))
        {
            superAdminProtection.EnsureCanEditAdminTarget(
                platformUserId.Value, currentUser.PlatformUserId ?? Guid.Empty,
                currentUser.HasPermission("platform.user.manageSuperAdmins"));
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        user.ChangeDisplayName(command.DisplayName, nowUtc);
        platformAuditLog.Add(PlatformAuditLogEntry.Record(
            nowUtc, currentUser.PlatformUserId, "PlatformUser.Update", nameof(PlatformUser), platformUserId.Value.ToString()));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Platform user {PlatformUserId} updated", platformUserId);

        return Unit.Value;
    }
}
