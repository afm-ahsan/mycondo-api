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

    void Add(RoleAssignment roleAssignment);
    void Remove(RoleAssignment roleAssignment);
}
