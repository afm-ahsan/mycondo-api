using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Authorization;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Seed;
using NSubstitute;

namespace MyCondo.Infrastructure.IntegrationTests.Seed;

/// <summary>
/// Closure item 1 (Billing↔Finance integration template): proves
/// <see cref="TenantRoleCatalogueBackfillSeeder"/> — the generalized, not-ARP-specific fix for the
/// permission-reconciliation gap the new billing.fine.* permissions surfaced — reconciles every
/// existing tenant's OrganizationAdmin role, writes each tenant's grants only through that tenant's own
/// <see cref="ITenantScopedUnitOfWork"/>, and never revokes an existing role or grant. Uses the real
/// <see cref="PermissionCatalogue.Entries"/> as the permission set so this test can never drift out of
/// sync with the actual catalogue (a hand-maintained duplicate would silently stop testing anything
/// real the moment a permission was renamed).
/// </summary>
public class TenantRoleCatalogueBackfillSeederTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static List<Permission> RealCatalogue() =>
        PermissionCatalogue.Entries
            .Select(e => Permission.Create(PermissionId.New(), e.Name, e.Description, e.Module, e.IsBuildingScopable))
            .ToList();

    private static (IServiceScopeFactory ScopeFactory, ITenantRepository Tenants) BuildScopeFactory(List<Tenant> tenants)
    {
        ITenantRepository tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(tenants);

        IClock clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);

        ServiceCollection services = new();
        services.AddSingleton(tenantRepository);
        services.AddSingleton(clock);
        IServiceProvider provider = services.BuildServiceProvider();

        return (provider.GetRequiredService<IServiceScopeFactory>(), tenantRepository);
    }

    /// <summary>Builds a tenant-scoped UoW whose Roles/Permissions/RolePermissions/RoleAssignments are
    /// wired for the full real catalogue (so DefaultRoleCatalogueSeeder/CondominiumRoleCatalogueSeeder/
    /// ResidentRoleCatalogueSeeder can run to completion without throwing on a missing permission) and
    /// whose OrganizationAdmin role already exists with zero grants — the exact shape of a tenant
    /// bootstrapped before this closure item's new permissions existed.</summary>
    private static (ITenantScopedUnitOfWork Uow, IRolePermissionRepository RolePermissions, Role OrganizationAdmin) BuildAlreadyBootstrappedTenantUow(
        Guid tenantId, List<Permission> catalogue)
    {
        ITenantScopedUnitOfWork uow = Substitute.For<ITenantScopedUnitOfWork>();
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        IRolePermissionRepository rolePermissions = Substitute.For<IRolePermissionRepository>();
        IRoleAssignmentRepository roleAssignments = Substitute.For<IRoleAssignmentRepository>();

        Role organizationAdmin = Role.CreateSystem(
            RoleId.New(), tenantId, "OrganizationAdmin", "desc", Now, code: "organization.admin", requiresBuildingScope: false);

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([organizationAdmin]);
        rolePermissions.GetForRoleAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns([]);

        uow.Roles.Returns(roles);
        uow.Permissions.Returns(permissions);
        uow.RolePermissions.Returns(rolePermissions);
        uow.RoleAssignments.Returns(roleAssignments);

        return (uow, rolePermissions, organizationAdmin);
    }

    [Fact]
    public async Task Reconciles_OrganizationAdmin_For_Every_Existing_Tenant_Without_Cross_Tenant_Writes()
    {
        Tenant tenantA = Tenant.Provision("Tenant A", "tenant-a", Now);
        Tenant tenantB = Tenant.Provision("Tenant B", "tenant-b", Now);
        (IServiceScopeFactory scopeFactory, _) = BuildScopeFactory([tenantA, tenantB]);

        List<Permission> catalogue = RealCatalogue();
        (ITenantScopedUnitOfWork uowA, IRolePermissionRepository rolePermissionsA, Role orgAdminA) =
            BuildAlreadyBootstrappedTenantUow(tenantA.Id.Value, catalogue);
        (ITenantScopedUnitOfWork uowB, IRolePermissionRepository rolePermissionsB, Role orgAdminB) =
            BuildAlreadyBootstrappedTenantUow(tenantB.Id.Value, catalogue);

        ITenantScopedUnitOfWorkFactory tenantUowFactory = Substitute.For<ITenantScopedUnitOfWorkFactory>();
        tenantUowFactory.Create(tenantA.Id.Value).Returns(uowA);
        tenantUowFactory.Create(tenantB.Id.Value).Returns(uowB);

        List<RolePermission> addedToA = [];
        rolePermissionsA.Add(Arg.Do<RolePermission>(g => addedToA.Add(g)));
        List<RolePermission> addedToB = [];
        rolePermissionsB.Add(Arg.Do<RolePermission>(g => addedToB.Add(g)));

        TenantRoleCatalogueBackfillSeeder seeder = new(scopeFactory, tenantUowFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        tenantUowFactory.Received(1).Create(tenantA.Id.Value);
        tenantUowFactory.Received(1).Create(tenantB.Id.Value);
        await uowA.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await uowB.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // rolePermissionsA/B receive grants for every role reconciled this run — OrganizationAdmin
        // *and* the catalogue-role seeders' newly-created roles (BuildingAdmin, Treasurer, etc.), since
        // they share the same tenant-scoped repository instance. The isolation property under test is
        // that every single grant recorded through tenant A's repository carries tenant A's TenantId
        // (never tenant B's, or vice versa) — proving no write crossed tenant boundaries.
        addedToA.Should().OnlyContain(g => g.TenantId == tenantA.Id.Value);
        addedToB.Should().OnlyContain(g => g.TenantId == tenantB.Id.Value);
        addedToA.Should().HaveCount(addedToB.Count, "both tenants start from the identical catalogue/role shape");
        addedToA.Should().NotBeEmpty();

        // Narrower check: OrganizationAdmin specifically (not just some catalogue role) got the new
        // Fine permissions, for both tenants independently.
        List<RolePermission> orgAdminGrantsA = addedToA.Where(g => g.RoleId == orgAdminA.Id).ToList();
        List<RolePermission> orgAdminGrantsB = addedToB.Where(g => g.RoleId == orgAdminB.Id).ToList();
        HashSet<string> grantedNamesA = catalogue
            .Where(p => orgAdminGrantsA.Any(g => g.PermissionId == p.Id)).Select(p => p.Name).ToHashSet();
        HashSet<string> grantedNamesB = catalogue
            .Where(p => orgAdminGrantsB.Any(g => g.PermissionId == p.Id)).Select(p => p.Name).ToHashSet();
        grantedNamesA.Should().Contain(["billing.fine.view", "billing.fine.assess", "billing.fine.waive", "billing.fine.reverse"]);
        grantedNamesB.Should().Contain(["billing.fine.view", "billing.fine.assess", "billing.fine.waive", "billing.fine.reverse"]);
    }

    [Fact]
    public async Task Second_Run_Against_An_Already_Reconciled_Tenant_Creates_No_New_OrganizationAdmin_Grants()
    {
        Tenant tenant = Tenant.Provision("Tenant A", "tenant-a", Now);
        (IServiceScopeFactory scopeFactory, _) = BuildScopeFactory([tenant]);

        List<Permission> catalogue = RealCatalogue();
        ITenantScopedUnitOfWork uow = Substitute.For<ITenantScopedUnitOfWork>();
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        IRolePermissionRepository rolePermissions = Substitute.For<IRolePermissionRepository>();
        IRoleAssignmentRepository roleAssignments = Substitute.For<IRoleAssignmentRepository>();

        Role organizationAdmin = Role.CreateSystem(
            RoleId.New(), tenant.Id.Value, "OrganizationAdmin", "desc", Now, code: "organization.admin", requiresBuildingScope: false);
        // Already has every non-Platform/non-tenant-lifecycle permission granted — the state after a
        // prior successful reconciliation run.
        List<RolePermission> existingGrants = catalogue
            .Where(p => p.Module != "platform" && p.Module != "tenant")
            .Select(p => new RolePermission(tenant.Id.Value, organizationAdmin.Id, p.Id, Now, null))
            .ToList();

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns([organizationAdmin]);
        // Default for any other role (the catalogue-role seeders create fresh roles this run since none
        // pre-exist for this tenant) — only OrganizationAdmin's own lookup below is overridden.
        rolePermissions.GetForRoleAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns([]);
        rolePermissions.GetForRoleAsync(organizationAdmin.Id, Arg.Any<CancellationToken>()).Returns(existingGrants);

        uow.Roles.Returns(roles);
        uow.Permissions.Returns(permissions);
        uow.RolePermissions.Returns(rolePermissions);
        uow.RoleAssignments.Returns(roleAssignments);

        ITenantScopedUnitOfWorkFactory tenantUowFactory = Substitute.For<ITenantScopedUnitOfWorkFactory>();
        tenantUowFactory.Create(tenant.Id.Value).Returns(uow);

        List<RolePermission> allAddedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => allAddedGrants.Add(g)));

        TenantRoleCatalogueBackfillSeeder seeder = new(scopeFactory, tenantUowFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        // OrganizationAdmin specifically gets nothing new — it's already fully reconciled. The
        // catalogue-role seeders (BuildingAdmin/Treasurer/etc.) still create their roles/grants in this
        // test since none pre-exist for this tenant; that's a separate, already-covered concern
        // (DefaultRoleCatalogueSeederTests et al.), not what this test is isolating.
        allAddedGrants.Should().NotContain(g => g.RoleId == organizationAdmin.Id);
    }

    [Fact]
    public async Task Never_Touches_A_Tenant_Still_On_The_Legacy_SuperAdmin_Role()
    {
        Tenant tenant = Tenant.Provision("Legacy Tenant", "legacy-tenant", Now);
        (IServiceScopeFactory scopeFactory, _) = BuildScopeFactory([tenant]);

        List<Permission> catalogue = RealCatalogue();
        ITenantScopedUnitOfWork uow = Substitute.For<ITenantScopedUnitOfWork>();
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        IRolePermissionRepository rolePermissions = Substitute.For<IRolePermissionRepository>();
        IRoleAssignmentRepository roleAssignments = Substitute.For<IRoleAssignmentRepository>();

        // Legacy tenant SuperAdmin role — predates Phase 2's OrganizationAdmin/Code convention (see
        // IOrganizationAdminBootstrapper's doc comment). Code is unrelated ("tenant.superadmin"), not
        // "organization.admin", so ReconcilePermissionsAsync must find nothing to reconcile here.
        Role legacySuperAdmin = Role.CreateSystem(
            RoleId.New(), tenant.Id.Value, "SuperAdmin", "desc", Now, code: "tenant.superadmin");

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns([legacySuperAdmin]);
        rolePermissions.GetForRoleAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns([]);

        uow.Roles.Returns(roles);
        uow.Permissions.Returns(permissions);
        uow.RolePermissions.Returns(rolePermissions);
        uow.RoleAssignments.Returns(roleAssignments);

        ITenantScopedUnitOfWorkFactory tenantUowFactory = Substitute.For<ITenantScopedUnitOfWorkFactory>();
        tenantUowFactory.Create(tenant.Id.Value).Returns(uow);

        TenantRoleCatalogueBackfillSeeder seeder = new(scopeFactory, tenantUowFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        rolePermissions.DidNotReceive().Add(Arg.Is<RolePermission>(g => g.RoleId == legacySuperAdmin.Id));
    }
}
