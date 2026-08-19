using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

/// <summary>
/// Focused tests for <see cref="OrganizationAdminBootstrapper.ReconcilePermissionsAsync"/> — the
/// generalized (not ARP-specific) fix for the gap the Billing↔Finance integration template's new
/// billing.fine.* permissions surfaced: <c>BootstrapAsync</c> only ever runs once, at a tenant's
/// first-user registration, so a permission added to the catalogue afterward never reached an
/// already-bootstrapped tenant's OrganizationAdmin role. See <see cref="TenantRoleCatalogueBackfillSeeder"/>
/// (Infrastructure) for the every-tenant, every-environment orchestration this method plugs into.
/// </summary>
public class OrganizationAdminBootstrapperReconciliationTests
{
    private const string OrganizationAdminCode = "organization.admin";

    private static List<Permission> BuildCatalogue(params (string Name, string Module)[] entries) =>
        entries.Select(e => Permission.Create(PermissionId.New(), e.Name, e.Name, e.Module)).ToList();

    private static (IRoleRepository Roles, IPermissionRepository Permissions, IRolePermissionRepository RolePermissions,
        IRoleAssignmentRepository RoleAssignments, ILogger<OrganizationAdminBootstrapper> Logger) BuildSubstitutes() =>
        (Substitute.For<IRoleRepository>(), Substitute.For<IPermissionRepository>(),
            Substitute.For<IRolePermissionRepository>(), Substitute.For<IRoleAssignmentRepository>(),
            Substitute.For<ILogger<OrganizationAdminBootstrapper>>());

    [Fact]
    public async Task ReconcilePermissionsAsync_Grants_A_Newly_Added_Tenant_Permission_To_An_Existing_OrganizationAdmin()
    {
        Guid tenantId = Guid.NewGuid();
        List<Permission> catalogue = BuildCatalogue(
            ("tenant.view", "tenant"), ("billing.fine.view", "billing"), ("billing.fine.assess", "billing"));
        Role organizationAdmin = Role.CreateSystem(
            RoleId.New(), tenantId, "OrganizationAdmin", "desc", DateTimeOffset.UtcNow,
            code: OrganizationAdminCode, requiresBuildingScope: false);

        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions,
            IRoleAssignmentRepository roleAssignments, ILogger<OrganizationAdminBootstrapper> logger) = BuildSubstitutes();

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([organizationAdmin]);
        // Already has the pre-existing "billing.fine.view" grant from a prior run; "billing.fine.assess"
        // is the newly-added permission it must now pick up.
        Permission existingGrantPermission = catalogue.Single(p => p.Name == "billing.fine.view");
        rolePermissions.GetForRoleAsync(organizationAdmin.Id, Arg.Any<CancellationToken>())
            .Returns([new RolePermission(tenantId, organizationAdmin.Id, existingGrantPermission.Id, DateTimeOffset.UtcNow, null)]);

        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        OrganizationAdminBootstrapper bootstrapper = new(roles, permissions, rolePermissions, roleAssignments, logger);
        int grantsCreated = await bootstrapper.ReconcilePermissionsAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        grantsCreated.Should().Be(1);
        addedGrants.Should().ContainSingle(g => g.PermissionId == catalogue.Single(p => p.Name == "billing.fine.assess").Id);
        // tenant.view is a tenant-lifecycle-module permission — must never be granted to OrganizationAdmin.
        addedGrants.Should().NotContain(g => g.PermissionId == catalogue.Single(p => p.Name == "tenant.view").Id);
    }

    [Fact]
    public async Task ReconcilePermissionsAsync_Excludes_Platform_And_Tenant_Lifecycle_Modules()
    {
        Guid tenantId = Guid.NewGuid();
        List<Permission> catalogue = BuildCatalogue(
            ("platform.tenant.manage", "platform"), ("tenant.manage", "tenant"), ("billing.fine.view", "billing"));
        Role organizationAdmin = Role.CreateSystem(
            RoleId.New(), tenantId, "OrganizationAdmin", "desc", DateTimeOffset.UtcNow,
            code: OrganizationAdminCode, requiresBuildingScope: false);

        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions,
            IRoleAssignmentRepository roleAssignments, ILogger<OrganizationAdminBootstrapper> logger) = BuildSubstitutes();

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([organizationAdmin]);
        rolePermissions.GetForRoleAsync(organizationAdmin.Id, Arg.Any<CancellationToken>()).Returns([]);

        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        OrganizationAdminBootstrapper bootstrapper = new(roles, permissions, rolePermissions, roleAssignments, logger);
        int grantsCreated = await bootstrapper.ReconcilePermissionsAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        grantsCreated.Should().Be(1);
        addedGrants.Should().ContainSingle(g => g.PermissionId == catalogue.Single(p => p.Name == "billing.fine.view").Id);
    }

    [Fact]
    public async Task ReconcilePermissionsAsync_Is_A_NoOp_When_Tenant_Has_No_OrganizationAdmin_Role()
    {
        // Covers both a tenant not yet bootstrapped and a tenant still on the legacy SuperAdmin role
        // (Code != "organization.admin") — see IOrganizationAdminBootstrapper's doc comment: legacy
        // SuperAdmin tenants must be left completely untouched, same policy as BootstrapAsync.
        Guid tenantId = Guid.NewGuid();
        List<Permission> catalogue = BuildCatalogue(("billing.fine.view", "billing"));
        Role legacySuperAdmin = Role.CreateSystem(
            RoleId.New(), tenantId, "SuperAdmin", "desc", DateTimeOffset.UtcNow, code: "tenant.superadmin");

        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions,
            IRoleAssignmentRepository roleAssignments, ILogger<OrganizationAdminBootstrapper> logger) = BuildSubstitutes();

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([legacySuperAdmin]);

        OrganizationAdminBootstrapper bootstrapper = new(roles, permissions, rolePermissions, roleAssignments, logger);
        int grantsCreated = await bootstrapper.ReconcilePermissionsAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        grantsCreated.Should().Be(0);
        rolePermissions.DidNotReceive().Add(Arg.Any<RolePermission>());
    }

    [Fact]
    public async Task ReconcilePermissionsAsync_Creates_No_Grants_When_Already_Fully_Reconciled()
    {
        Guid tenantId = Guid.NewGuid();
        List<Permission> catalogue = BuildCatalogue(("billing.fine.view", "billing"));
        Role organizationAdmin = Role.CreateSystem(
            RoleId.New(), tenantId, "OrganizationAdmin", "desc", DateTimeOffset.UtcNow,
            code: OrganizationAdminCode, requiresBuildingScope: false);

        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions,
            IRoleAssignmentRepository roleAssignments, ILogger<OrganizationAdminBootstrapper> logger) = BuildSubstitutes();

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([organizationAdmin]);
        rolePermissions.GetForRoleAsync(organizationAdmin.Id, Arg.Any<CancellationToken>())
            .Returns([new RolePermission(tenantId, organizationAdmin.Id, catalogue[0].Id, DateTimeOffset.UtcNow, null)]);

        OrganizationAdminBootstrapper bootstrapper = new(roles, permissions, rolePermissions, roleAssignments, logger);
        int grantsCreated = await bootstrapper.ReconcilePermissionsAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        grantsCreated.Should().Be(0);
        rolePermissions.DidNotReceive().Add(Arg.Any<RolePermission>());
    }
}
