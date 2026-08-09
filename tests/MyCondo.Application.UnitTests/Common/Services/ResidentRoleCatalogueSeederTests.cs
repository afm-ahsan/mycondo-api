using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

/// <summary>
/// Directly proves the FlatOwner/Tenant authorization boundary the refactor spec (§8/§18) calls out by
/// name — neither resident role receives <c>role.manage</c>/<c>role.view</c>/<c>permission.view</c> or
/// any relationship/administrative-management permission, no matter how many times this seeder runs.
/// mycondo-docs ADR-021 is the source of truth for the intended permission lists.
/// </summary>
public class ResidentRoleCatalogueSeederTests
{
    private static readonly string[] CatalogueNames =
    [
        "ownership.view", "ownership.manage", "lease.view", "lease.manage", "invoice.view",
        "invoice.view.own", "role.manage", "role.view", "permission.view",
    ];

    private static List<Permission> BuildCatalogue() =>
        CatalogueNames.Select(n => Permission.Create(PermissionId.New(), n, n, n.Split('.')[0])).ToList();

    private static (IRoleRepository Roles, IPermissionRepository Permissions, IRolePermissionRepository RolePermissions, ILogger<ResidentRoleCatalogueSeeder> Logger, List<Permission> Catalogue)
        BuildSubstitutes(IEnumerable<Role>? existingRoles = null)
    {
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        IRolePermissionRepository rolePermissions = Substitute.For<IRolePermissionRepository>();
        ILogger<ResidentRoleCatalogueSeeder> logger = Substitute.For<ILogger<ResidentRoleCatalogueSeeder>>();

        List<Permission> catalogue = BuildCatalogue();
        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((existingRoles ?? []).ToList());
        rolePermissions.GetForRoleAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns([]);

        return (roles, permissions, rolePermissions, logger, catalogue);
    }

    [Fact]
    public async Task SeedAsync_Grants_FlatOwner_And_Tenant_Exactly_Their_Documented_Permissions()
    {
        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions, ILogger<ResidentRoleCatalogueSeeder> logger, List<Permission> catalogue) =
            BuildSubstitutes();

        List<Role> addedRoles = [];
        roles.Add(Arg.Do<Role>(r => addedRoles.Add(r)));
        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        ResidentRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        Guid tenantId = Guid.NewGuid();
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        addedRoles.Select(r => r.Name).Should().BeEquivalentTo(["FlatOwner", "Tenant"]);
        addedRoles.Should().OnlyContain(r => r.IsSystem && r.RequiresBuildingScope == true);

        Role flatOwner = addedRoles.Single(r => r.Code == "resident.flat-owner");
        Role tenant = addedRoles.Single(r => r.Code == "resident.tenant");

        string PermissionNameOf(RolePermission grant) => catalogue.Single(p => p.Id == grant.PermissionId).Name;

        addedGrants.Where(g => g.RoleId == flatOwner.Id).Select(PermissionNameOf)
            .Should().BeEquivalentTo(["ownership.view", "invoice.view.own"]);
        addedGrants.Where(g => g.RoleId == tenant.Id).Select(PermissionNameOf)
            .Should().BeEquivalentTo(["lease.view", "invoice.view.own"]);

        addedGrants.Select(PermissionNameOf).Should().NotContain(["role.manage", "role.view", "permission.view", "ownership.manage", "lease.manage"]);
    }

    [Fact]
    public async Task SeedAsync_Reconciles_Without_Ever_Granting_Management_Permissions_On_Rerun()
    {
        Guid tenantId = Guid.NewGuid();
        Role existingFlatOwner = Role.CreateSystem(
            RoleId.New(), tenantId, "FlatOwner", "desc", DateTimeOffset.UtcNow,
            code: "resident.flat-owner", requiresBuildingScope: true);
        Role existingTenant = Role.CreateSystem(
            RoleId.New(), tenantId, "Tenant", "desc", DateTimeOffset.UtcNow,
            code: "resident.tenant", requiresBuildingScope: true);

        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions, ILogger<ResidentRoleCatalogueSeeder> logger, List<Permission> catalogue) =
            BuildSubstitutes([existingFlatOwner, existingTenant]);

        List<Role> addedRoles = [];
        roles.Add(Arg.Do<Role>(r => addedRoles.Add(r)));
        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        ResidentRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        // Both roles already existed by Code — reconciliation must not create duplicates, only backfill
        // their (still-empty, per this test's stub) expected grants.
        addedRoles.Should().BeEmpty();
        addedGrants.Should().HaveCount(4);

        addedGrants.Select(g => catalogue.Single(p => p.Id == g.PermissionId).Name)
            .Should().NotContain(["role.manage", "role.view", "permission.view"]);
    }
}
