using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Platform-scope analogue of <see cref="ITenantAdminProtectionService"/> — enforces the
/// <c>platform.user.manageSuperAdmins</c> composition permission, self-protection, and the race-safe
/// last-active-Super-Admin invariant for the Platform "SuperAdmin" role. See mycondo-docs ADR-035.
/// </summary>
public interface IPlatformSuperAdminProtectionService
{
    bool IsSuperAdmin(PlatformRole role);

    /// <summary>True if the given Platform user currently holds the SuperAdmin role.</summary>
    Task<bool> TargetIsSuperAdminAsync(PlatformUserId targetId, CancellationToken cancellationToken);

    /// <summary>
    /// Throws <see cref="Exceptions.ForbiddenException"/> if the actor is targeting their own account, or
    /// if the actor lacks <c>platform.user.manageSuperAdmins</c>. Call only when the target holds the
    /// SuperAdmin role.
    /// </summary>
    void EnsureCanMutateAdminTarget(Guid targetPlatformUserId, Guid actorPlatformUserId, bool actorCanManageSuperAdmins);

    /// <summary>
    /// Throws <see cref="Exceptions.ForbiddenException"/> if the actor lacks
    /// <c>platform.user.manageSuperAdmins</c> — unless the actor is editing their own account, which is
    /// always allowed. For non-destructive edits (e.g. updating display name) where self-action is
    /// permitted but a lower-privileged actor must still be blocked from editing another Super Admin.
    /// Call only when the target holds the SuperAdmin role.
    /// </summary>
    void EnsureCanEditAdminTarget(Guid targetPlatformUserId, Guid actorPlatformUserId, bool actorCanManageSuperAdmins);

    /// <summary>
    /// Throws <see cref="Exceptions.ConflictException"/> if deactivating/demoting the target would leave
    /// zero active Platform Super Admins. Race-safe: must be called inside an explicit
    /// <see cref="MyCondo.Domain.Abstractions.IUnitOfWork"/> transaction. Call only when the target holds
    /// the SuperAdmin role.
    /// </summary>
    Task EnsureNotLastActiveSuperAdminAsync(CancellationToken cancellationToken);
}
