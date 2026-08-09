using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Infrastructure.Persistence.Seeding.Extensions;

/// <summary>Idempotent role/permission provisioning for seeding. Reuses the same authorization
/// services the HTTP registration path uses (<see cref="ISuperAdminBootstrapper"/>,
/// <see cref="IDefaultRoleCatalogueSeeder"/>) rather than duplicating role/permission-grant logic —
/// those services are not idempotent on their own (each unconditionally creates roles), so every
/// entry point here checks for the resulting role by name first.</summary>
internal static class RoleSeedExtensions
{
    private const string SuperAdminRoleName = "SuperAdmin";

    /// <summary>Ensures <paramref name="user"/> holds the tenant's SuperAdmin role, bootstrapping the
    /// role (and granting it every catalogue permission) only if the tenant doesn't have one yet.</summary>
    public static async Task EnsureSuperAdminAsync(
        this IRoleRepository roles,
        IRoleAssignmentRepository roleAssignments,
        ISuperAdminBootstrapper superAdminBootstrapper,
        Guid tenantId,
        User user,
        IClock clock,
        CancellationToken cancellationToken)
    {
        Role? existingRole = await roles.GetByNameAsync(tenantId, SuperAdminRoleName, cancellationToken);
        if (existingRole is null)
        {
            await superAdminBootstrapper.BootstrapAsync(tenantId, user, clock.UtcNow, cancellationToken);
            return;
        }

        await EnsureRoleAssignmentAsync(roleAssignments, tenantId, user, existingRole, clock, cancellationToken);
    }

    /// <summary>Ensures the tenant's default custom role catalogue (BuildingAdmin, Treasurer,
    /// Secretary, SecurityHead, Owner, Renter, Auditor) exists, probing for one representative role
    /// rather than re-seeding all seven every time.</summary>
    public static async Task EnsureDefaultRoleCatalogueAsync(
        this IRoleRepository roles,
        IDefaultRoleCatalogueSeeder defaultRoleCatalogueSeeder,
        Guid tenantId,
        string probeRoleName,
        IClock clock,
        CancellationToken cancellationToken)
    {
        Role? probe = await roles.GetByNameAsync(tenantId, probeRoleName, cancellationToken);
        if (probe is null)
        {
            await defaultRoleCatalogueSeeder.SeedAsync(tenantId, clock.UtcNow, cancellationToken);
        }
    }

    /// <summary>Ensures <paramref name="user"/> holds a tenant-wide (non-building-scoped) grant of the
    /// named role. The role itself must already exist (e.g. via <see cref="EnsureDefaultRoleCatalogueAsync"/>).</summary>
    public static async Task EnsureRoleAssignmentAsync(
        this IRoleRepository roles,
        IRoleAssignmentRepository roleAssignments,
        Guid tenantId,
        User user,
        string roleName,
        IClock clock,
        CancellationToken cancellationToken)
    {
        Role role = await roles.GetByNameAsync(tenantId, roleName, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Seeding expected role '{roleName}' to already exist for tenant {tenantId}.");

        await EnsureRoleAssignmentAsync(roleAssignments, tenantId, user, role, clock, cancellationToken);
    }

    private static async Task EnsureRoleAssignmentAsync(
        IRoleAssignmentRepository roleAssignments,
        Guid tenantId,
        User user,
        Role role,
        IClock clock,
        CancellationToken cancellationToken)
    {
        bool alreadyAssigned = await roleAssignments.ExistsAsync(
            tenantId, user.Id, role.Id, buildingId: null, cancellationToken);

        if (!alreadyAssigned)
        {
            roleAssignments.Add(RoleAssignment.Grant(tenantId, user.Id, role.Id, buildingId: null, clock.UtcNow));
        }
    }
}
