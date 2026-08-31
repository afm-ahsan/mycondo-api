using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Audit;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Users.Commands.DeactivateUser;

public sealed class DeactivateUserCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ITenantAdminProtectionService tenantAdminProtection,
    IIdentityAuditLogRepository identityAuditLog,
    IClock clock,
    ILogger<DeactivateUserCommandHandler> logger
) : IRequestHandler<DeactivateUserCommand>
{
    public async ValueTask<Unit> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
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

        // mycondo-docs ADR-036 — a Tenant Admin cannot self-disable, a lower-privileged actor cannot
        // disable a Tenant Admin, and the tenant's last active Tenant Admin can never be disabled.
        bool targetIsTenantAdmin = await tenantAdminProtection.TargetHoldsTenantAdminRoleAsync(
            tenantId, userId, cancellationToken);

        if (targetIsTenantAdmin)
        {
            tenantAdminProtection.EnsureCanMutateAdminTarget(
                userId.Value, currentUser.UserId ?? Guid.Empty, currentUser.HasPermission("user.manageTenantAdmins"));
        }

        await using IUnitOfWorkTransaction? transaction = targetIsTenantAdmin
            ? await unitOfWork.BeginTransactionAsync(cancellationToken)
            : null;

        if (targetIsTenantAdmin)
        {
            await tenantAdminProtection.EnsureNotLastActiveAdminAsync(tenantId, userId, cancellationToken);
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        user.Deactivate(nowUtc);
        identityAuditLog.Add(IdentityAuditLogEntry.Record(
            tenantId, nowUtc, currentUser.UserId, "User.Deactivate", nameof(User), userId.Value.ToString()));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        logger.LogInformation("User {UserId} deactivated for tenant {TenantId}", userId, tenantId);

        return Unit.Value;
    }
}
