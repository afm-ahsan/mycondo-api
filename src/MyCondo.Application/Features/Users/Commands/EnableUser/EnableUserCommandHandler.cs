using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Audit;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Users.Commands.EnableUser;

public sealed class EnableUserCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ITenantAdminProtectionService tenantAdminProtection,
    IIdentityAuditLogRepository identityAuditLog,
    IClock clock,
    ILogger<EnableUserCommandHandler> logger
) : IRequestHandler<EnableUserCommand>
{
    public async ValueTask<Unit> Handle(EnableUserCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        UserId userId = new(command.UserId);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), command.UserId);

        if (user.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(User), command.UserId);
        }

        // mycondo-docs ADR-036 — re-activating never reduces the tenant's active-admin count, so no
        // last-admin check here, only the target-privilege composition (self always allowed).
        if (await tenantAdminProtection.TargetHoldsTenantAdminRoleAsync(tenantId, userId, cancellationToken))
        {
            tenantAdminProtection.EnsureCanEditAdminTarget(
                userId.Value, currentUser.UserId ?? Guid.Empty, currentUser.HasPermission("user.manageTenantAdmins"));
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        user.Activate(nowUtc);
        identityAuditLog.Add(IdentityAuditLogEntry.Record(
            tenantId, nowUtc, currentUser.UserId, "User.Activate", nameof(User), userId.Value.ToString()));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} enabled for tenant {TenantId}", userId, tenantId);

        return Unit.Value;
    }
}
