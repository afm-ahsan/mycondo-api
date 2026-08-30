using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Audit;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ITenantAdminProtectionService tenantAdminProtection,
    IIdentityAuditLogRepository identityAuditLog,
    IClock clock,
    ILogger<UpdateUserCommandHandler> logger
) : IRequestHandler<UpdateUserCommand>
{
    public async ValueTask<Unit> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
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

        // mycondo-docs ADR-035 — a Tenant Admin may always edit its own profile; a lower-privileged
        // actor may never edit another Tenant Admin's profile.
        if (await tenantAdminProtection.TargetHoldsTenantAdminRoleAsync(tenantId, userId, cancellationToken))
        {
            tenantAdminProtection.EnsureCanEditAdminTarget(
                userId.Value, currentUser.UserId ?? Guid.Empty, currentUser.HasPermission("user.manageTenantAdmins"));
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        user.UpdateProfile(command.FullName, command.PhoneNumber, nowUtc);
        identityAuditLog.Add(IdentityAuditLogEntry.Record(
            tenantId, nowUtc, currentUser.UserId, "User.Update", nameof(User), userId.Value.ToString()));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated for tenant {TenantId}", userId, tenantId);

        return Unit.Value;
    }
}
