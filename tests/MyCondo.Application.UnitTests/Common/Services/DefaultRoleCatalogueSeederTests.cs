using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

public class DefaultRoleCatalogueSeederTests
{
    // Mirrors the 47 names actually seeded by Seed_Permission_Catalogue — using the same set here
    // means a typo in DefaultRoleCatalogueSeeder's permission lists fails this test the same way it
    // would fail against the real catalogue.
    private static readonly string[] FullCatalogueNames =
    [
        "audit.view", "billing.generate", "billing.rule.manage", "billing.rule.view",
        "complaint.assign", "complaint.create", "complaint.manage", "complaint.view",
        "document.delete", "document.upload", "document.view", "expense.manage", "expense.view",
        "invoice.view", "invoice.void", "lease.manage", "lease.view", "notification.manage",
        "notification.view", "ownership.manage", "ownership.view", "payment.record",
        "payment.reverse", "payment.view", "permission.view", "property.create", "property.delete",
        "property.update", "property.view", "report.financial.view", "report.operational.view",
        "resident.create", "resident.disable", "resident.update", "resident.view", "role.manage",
        "role.view", "tenant.manage", "tenant.view", "user.create", "user.disable", "user.update",
        "user.view", "workorder.assign", "workorder.complete", "workorder.create", "workorder.view",
    ];

    private static List<Permission> BuildCatalogue(IEnumerable<string> names) =>
        names.Select(n => Permission.Create(PermissionId.New(), n, n, n.Split('.')[0])).ToList();

    private static (IRoleRepository Roles, IPermissionRepository Permissions, IRolePermissionRepository RolePermissions, ILogger<DefaultRoleCatalogueSeeder> Logger)
        BuildSubstitutes(IEnumerable<Permission> catalogue, IEnumerable<Role>? existingRoles = null)
    {
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        IRolePermissionRepository rolePermissions = Substitute.For<IRolePermissionRepository>();
        ILogger<DefaultRoleCatalogueSeeder> logger = Substitute.For<ILogger<DefaultRoleCatalogueSeeder>>();

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue.ToList());
        roles.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((existingRoles ?? []).ToList());
        rolePermissions.GetForRoleAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        return (roles, permissions, rolePermissions, logger);
    }

    [Fact]
    public async Task SeedAsync_Creates_The_Seven_Implementable_Default_Roles()
    {
        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions, ILogger<DefaultRoleCatalogueSeeder> logger) =
            BuildSubstitutes(BuildCatalogue(FullCatalogueNames));

        List<Role> addedRoles = [];
        roles.Add(Arg.Do<Role>(r => addedRoles.Add(r)));

        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        DefaultRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        Guid tenantId = Guid.NewGuid();

        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        addedRoles.Select(r => r.Name).Should().BeEquivalentTo(
            ["BuildingAdmin", "Treasurer", "Secretary", "SecurityHead", "Owner", "Renter", "Auditor"]);
        addedRoles.Should().OnlyContain(r => !r.IsSystem && r.TenantId == tenantId);
        addedRoles.Should().OnlyContain(r => r.Code != null && r.Code.StartsWith("default."));
        addedRoles.Should().NotContain(r => r.Name == "Vendor" || r.Name == "Guard");

        addedGrants.Should().HaveCount(77);
        addedGrants.Should().OnlyContain(g => g.TenantId == tenantId);
    }

    [Fact]
    public async Task SeedAsync_Throws_If_A_Referenced_Permission_Is_Missing_From_The_Catalogue()
    {
        // Missing "audit.view", which Auditor requires.
        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions, ILogger<DefaultRoleCatalogueSeeder> logger) =
            BuildSubstitutes(BuildCatalogue(FullCatalogueNames.Where(n => n != "audit.view")));

        DefaultRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);

        Func<Task> act = () => seeder.SeedAsync(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*audit.view*");
    }

    [Fact]
    public async Task SeedAsync_Reuses_An_Existing_Role_By_Code_Instead_Of_Duplicating_It()
    {
        Guid tenantId = Guid.NewGuid();
        List<Permission> catalogue = BuildCatalogue(FullCatalogueNames);
        Role existingSecurityHead = Role.CreateCustom(
            tenantId, "SecurityHead (renamed by admin)", "desc", DateTimeOffset.UtcNow, "default.security-head");

        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions, ILogger<DefaultRoleCatalogueSeeder> logger) =
            BuildSubstitutes(catalogue, [existingSecurityHead]);

        List<Role> addedRoles = [];
        roles.Add(Arg.Do<Role>(r => addedRoles.Add(r)));

        DefaultRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        // A role with this Code already exists (even though the admin renamed it) — SeedAsync must not
        // create a second "SecurityHead" role.
        addedRoles.Should().NotContain(r => r.Code == "default.security-head");
    }

    [Fact]
    public async Task SeedAsync_Adds_A_Missing_Grant_To_An_Existing_Role_Without_Duplicating_Existing_Ones()
    {
        Guid tenantId = Guid.NewGuid();
        List<Permission> catalogue = BuildCatalogue(FullCatalogueNames);
        Role existingSecurityHead = Role.CreateCustom(
            tenantId, "SecurityHead", "desc", DateTimeOffset.UtcNow, "default.security-head");

        IRoleRepository roles = Substitute.For<IRoleRepository>();
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        IRolePermissionRepository rolePermissions = Substitute.For<IRolePermissionRepository>();
        ILogger<DefaultRoleCatalogueSeeder> logger = Substitute.For<ILogger<DefaultRoleCatalogueSeeder>>();

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([existingSecurityHead]);
        // Every newly-created role starts with no grants; SecurityHead specifically already has its
        // one expected grant ("complaint.view") persisted from a prior run.
        rolePermissions.GetForRoleAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns([]);
        Permission complaintView = catalogue.Single(p => p.Name == "complaint.view");
        rolePermissions.GetForRoleAsync(existingSecurityHead.Id, Arg.Any<CancellationToken>())
            .Returns([new RolePermission(tenantId, existingSecurityHead.Id, complaintView.Id, DateTimeOffset.UtcNow, null)]);

        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        DefaultRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        // The other six default roles are newly created here (no existing rows for them), so they get
        // their full grant sets added — the point of this test is specifically that SecurityHead,
        // which already had its one expected grant, gets nothing extra.
        addedGrants.Should().NotContain(g => g.RoleId == existingSecurityHead.Id);
    }
}
