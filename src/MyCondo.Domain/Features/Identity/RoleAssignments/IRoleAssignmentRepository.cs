using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Domain.Features.Identity.RoleAssignments;

public interface IRoleAssignmentRepository
{
    Task<bool> ExistsAsync(
        Guid tenantId,
        UserId userId,
        RoleId roleId,
        Guid? buildingId,
        CancellationToken cancellationToken);

    Task<RoleAssignment?> GetAsync(
        Guid tenantId,
        UserId userId,
        RoleId roleId,
        Guid? buildingId,
        CancellationToken cancellationToken);

    /// <summary>Distinct users holding a tenant-wide (non-building-scoped) assignment of this role —
    /// used to guard against revoking a tenant's last holder of a system role.</summary>
    Task<int> CountTenantWideHoldersAsync(Guid tenantId, RoleId roleId, CancellationToken cancellationToken);

    /// <summary>
    /// Race-safe counterpart to <see cref="CountTenantWideHoldersAsync"/> — locks (<c>FOR UPDATE</c>)
    /// every tenant-wide holder row for this role before counting, so a concurrent request against the
    /// same role's holder set must wait for this transaction to commit/roll back before it can read a
    /// consistent count. Must be called inside an explicit <see cref="MyCondo.Domain.Abstractions.IUnitOfWork"/>
    /// transaction (see mycondo-docs ADR-035 — last-active-admin invariant).
    /// </summary>
    Task<int> LockAndCountTenantWideHoldersAsync(Guid tenantId, RoleId roleId, CancellationToken cancellationToken);

    Task<List<RoleAssignment>> GetForRoleAsync(Guid tenantId, RoleId roleId, CancellationToken cancellationToken);

    Task<List<RoleAssignment>> GetForUserAsync(Guid tenantId, UserId userId, CancellationToken cancellationToken);

    /// <summary>Batched lookup for the User Administration list's Roles column — avoids one query per
    /// row when rendering a page of users.</summary>
    Task<List<RoleAssignment>> GetForUsersAsync(
        Guid tenantId, IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken);

    void Add(RoleAssignment roleAssignment);
    void Remove(RoleAssignment roleAssignment);
}
