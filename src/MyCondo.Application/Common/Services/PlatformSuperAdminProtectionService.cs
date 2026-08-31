using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Common.Services;

public sealed class PlatformSuperAdminProtectionService(
    IPlatformRoleRepository platformRoles,
    IPlatformUserRoleAssignmentRepository platformUserRoleAssignments
) : IPlatformSuperAdminProtectionService
{
    private const string SuperAdminRoleName = "SuperAdmin";

    public bool IsSuperAdmin(PlatformRole role) =>
        string.Equals(role.Name, SuperAdminRoleName, StringComparison.Ordinal);

    public async Task<bool> TargetIsSuperAdminAsync(PlatformUserId targetId, CancellationToken cancellationToken)
    {
        PlatformRole? superAdminRole = await platformRoles.GetByNameAsync(SuperAdminRoleName, cancellationToken);
        if (superAdminRole is null)
        {
            return false;
        }

        return await platformUserRoleAssignments.ExistsAsync(targetId, superAdminRole.Id, cancellationToken);
    }

    public void EnsureCanMutateAdminTarget(
        Guid targetPlatformUserId, Guid actorPlatformUserId, bool actorCanManageSuperAdmins)
    {
        if (targetPlatformUserId == actorPlatformUserId)
        {
            throw new ForbiddenException("You cannot perform this action on your own account.");
        }

        if (!actorCanManageSuperAdmins)
        {
            throw new ForbiddenException("Only a Platform Super Admin can manage another Super Admin's account.");
        }
    }

    public void EnsureCanEditAdminTarget(
        Guid targetPlatformUserId, Guid actorPlatformUserId, bool actorCanManageSuperAdmins)
    {
        if (targetPlatformUserId == actorPlatformUserId)
        {
            return;
        }

        if (!actorCanManageSuperAdmins)
        {
            throw new ForbiddenException("Only a Platform Super Admin can manage another Super Admin's account.");
        }
    }

    public async Task EnsureNotLastActiveSuperAdminAsync(CancellationToken cancellationToken)
    {
        PlatformRole? superAdminRole = await platformRoles.GetByNameAsync(SuperAdminRoleName, cancellationToken);
        if (superAdminRole is null)
        {
            return;
        }

        int holders = await platformUserRoleAssignments.LockAndCountActiveSuperAdminHoldersAsync(
            superAdminRole.Id, cancellationToken);
        if (holders <= 1)
        {
            throw new ConflictException("Cannot remove this account — it is the last active Platform Super Admin.");
        }
    }
}
