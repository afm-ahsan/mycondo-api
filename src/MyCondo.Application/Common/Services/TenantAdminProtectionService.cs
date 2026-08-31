using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Common.Services;

public sealed class TenantAdminProtectionService(
    IRoleRepository roles,
    IRoleAssignmentRepository roleAssignments
) : ITenantAdminProtectionService
{
    public bool IsTenantAdminEquivalent(Role role) => role.IsSystem && role.RequiresBuildingScope != true;

    public async Task<bool> TargetHoldsTenantAdminRoleAsync(
        Guid tenantId, UserId targetUserId, CancellationToken cancellationToken)
    {
        List<RoleAssignment> assignments = await roleAssignments.GetForUserAsync(tenantId, targetUserId, cancellationToken);
        foreach (RoleAssignment assignment in assignments)
        {
            Role? role = await roles.GetByIdAsync(assignment.RoleId, cancellationToken);
            if (role is not null && IsTenantAdminEquivalent(role))
            {
                return true;
            }
        }

        return false;
    }

    public void EnsureCanMutateAdminTarget(Guid targetUserId, Guid actorUserId, bool actorCanManageTenantAdmins)
    {
        if (targetUserId == actorUserId)
        {
            throw new ForbiddenException("You cannot perform this action on your own account.");
        }

        if (!actorCanManageTenantAdmins)
        {
            throw new ForbiddenException("Only a Tenant Admin can manage another Tenant Admin's account.");
        }
    }

    public void EnsureCanEditAdminTarget(Guid targetUserId, Guid actorUserId, bool actorCanManageTenantAdmins)
    {
        if (targetUserId == actorUserId)
        {
            return;
        }

        if (!actorCanManageTenantAdmins)
        {
            throw new ForbiddenException("Only a Tenant Admin can manage another Tenant Admin's account.");
        }
    }

    public async Task EnsureNotLastActiveAdminAsync(
        Guid tenantId, UserId targetUserId, CancellationToken cancellationToken)
    {
        List<RoleAssignment> assignments = await roleAssignments.GetForUserAsync(tenantId, targetUserId, cancellationToken);

        foreach (RoleAssignment assignment in assignments.Where(a => a.BuildingId is null))
        {
            Role? role = await roles.GetByIdAsync(assignment.RoleId, cancellationToken);
            if (role is null || !IsTenantAdminEquivalent(role))
            {
                continue;
            }

            int holders = await roleAssignments.LockAndCountTenantWideHoldersAsync(
                tenantId, assignment.RoleId, cancellationToken);
            if (holders <= 1)
            {
                throw new ConflictException(
                    $"Cannot remove this account — it is the last active holder of the '{role.Name}' role for this tenant.");
            }
        }
    }
}
