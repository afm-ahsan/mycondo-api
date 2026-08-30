using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Enforces the privileged-target protection rules for tenant-wide, admin-equivalent system roles
/// (legacy <c>SuperAdmin</c> and <c>OrganizationAdmin</c> — any system role with
/// <c>RequiresBuildingScope != true</c>): the <c>user.manageTenantAdmins</c> composition permission,
/// self-protection, and the race-safe last-active-admin invariant. Composes with the coarse
/// action-permission check already enforced at the endpoint filter (<c>RequirePermission</c>) — this
/// service is the second, handler-level gate. See mycondo-docs ADR-035.
/// </summary>
public interface ITenantAdminProtectionService
{
    /// <summary>True for a system role with no building-scope requirement — the "tenant-wide
    /// admin-equivalent" predicate shared by every protection check below.</summary>
    bool IsTenantAdminEquivalent(Role role);

    /// <summary>True if the given user currently holds at least one tenant-wide admin-equivalent role.</summary>
    Task<bool> TargetHoldsTenantAdminRoleAsync(Guid tenantId, UserId targetUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Throws <see cref="Exceptions.ForbiddenException"/> if the actor is targeting their own account, or
    /// if the actor lacks <c>user.manageTenantAdmins</c>. For destructive/demoting actions (deactivate,
    /// revoke an admin-equivalent role) where self-action must always be rejected, even for an actor who
    /// otherwise holds <c>user.manageTenantAdmins</c>. Call only when the target holds a tenant-wide
    /// admin-equivalent role.
    /// </summary>
    void EnsureCanMutateAdminTarget(Guid targetUserId, Guid actorUserId, bool actorCanManageTenantAdmins);

    /// <summary>
    /// Throws <see cref="Exceptions.ForbiddenException"/> if the actor lacks <c>user.manageTenantAdmins</c>
    /// — unless the actor is editing their own account, which is always allowed (a Tenant Admin may view
    /// and edit its own profile). For non-destructive edits (e.g. updating name/phone) where self-action is
    /// permitted but a lower-privileged actor must still be blocked from editing another admin. Call only
    /// when the target holds a tenant-wide admin-equivalent role.
    /// </summary>
    void EnsureCanEditAdminTarget(Guid targetUserId, Guid actorUserId, bool actorCanManageTenantAdmins);

    /// <summary>
    /// Throws <see cref="Exceptions.ConflictException"/> if removing the target user's tenant-wide
    /// admin-equivalent role membership(s) — via deactivation of the user, not merely a single role
    /// revocation — would leave any of those roles with zero active holders in the tenant. Race-safe:
    /// must be called inside an explicit <see cref="MyCondo.Domain.Abstractions.IUnitOfWork"/> transaction.
    /// </summary>
    Task EnsureNotLastActiveAdminAsync(Guid tenantId, UserId targetUserId, CancellationToken cancellationToken);
}
