using System.Text.Json;
using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Audit;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Roles.Commands.RevokeRoleFromUser;

public sealed class RevokeRoleFromUserCommandHandler(
    IRoleRepository roles,
    IUserRepository users,
    IRoleAssignmentRepository roleAssignments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ITenantAdminProtectionService tenantAdminProtection,
    IIdentityAuditLogRepository identityAuditLog,
    IClock clock,
    ILogger<RevokeRoleFromUserCommandHandler> logger
) : IRequestHandler<RevokeRoleFromUserCommand>
{
    public async ValueTask<Unit> Handle(RevokeRoleFromUserCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        RoleId roleId = new(command.RoleId);
        Role role = await roles.GetByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), command.RoleId);

        if (role.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Role), command.RoleId);
        }

        UserId userId = new(command.UserId);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), command.UserId);

        if (user.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(User), command.UserId);
        }

        RoleAssignment assignment = await roleAssignments.GetAsync(
                tenantId, userId, roleId, command.BuildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(RoleAssignment), command.UserId);

        // mycondo-docs ADR-035 — revoking a tenant-wide, admin-equivalent role (demotion) is subject to
        // self-protection and the manageTenantAdmins composition permission, on top of the pre-existing
        // last-holder protection every tenant-wide system role has always had.
        bool isSystemTenantWide = role.IsSystem && command.BuildingId is null;
        bool isTenantAdminRole = isSystemTenantWide && tenantAdminProtection.IsTenantAdminEquivalent(role);

        if (isTenantAdminRole)
        {
            tenantAdminProtection.EnsureCanMutateAdminTarget(
                userId.Value, currentUser.UserId ?? Guid.Empty, currentUser.HasPermission("user.manageTenantAdmins"));
        }

        await using IUnitOfWorkTransaction? transaction = isSystemTenantWide
            ? await unitOfWork.BeginTransactionAsync(cancellationToken)
            : null;

        if (isSystemTenantWide)
        {
            int holders = await roleAssignments.LockAndCountTenantWideHoldersAsync(tenantId, roleId, cancellationToken);
            if (holders <= 1)
            {
                throw new ConflictException(
                    $"Cannot revoke role '{role.Name}' — this tenant would be left with no one holding it.");
            }
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        roleAssignments.Remove(assignment);
        identityAuditLog.Add(IdentityAuditLogEntry.Record(
            tenantId, nowUtc, currentUser.UserId, "User.Role.Revoke", nameof(User), userId.Value.ToString(),
            metadata: JsonSerializer.Serialize(new { roleId = roleId.Value, roleName = role.Name, buildingId = command.BuildingId })));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        logger.LogInformation(
            "Role {RoleId} revoked from user {UserId} for tenant {TenantId}", roleId, userId, tenantId);

        return Unit.Value;
    }
}
