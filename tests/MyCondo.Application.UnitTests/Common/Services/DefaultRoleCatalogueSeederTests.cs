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

    [Fact]
    public async Task SeedAsync_Creates_The_Seven_Implementable_Default_Roles()
    {
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        IRolePermissionRepository rolePermissions = Substitute.For<IRolePermissionRepository>();
        ILogger<DefaultRoleCatalogueSeeder> logger = Substitute.For<ILogger<DefaultRoleCatalogueSeeder>>();

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(BuildCatalogue(FullCatalogueNames));

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
        addedRoles.Should().NotContain(r => r.Name == "Vendor" || r.Name == "Guard");

        addedGrants.Should().HaveCount(77);
        addedGrants.Should().OnlyContain(g => g.TenantId == tenantId);
    }

    [Fact]
    public async Task SeedAsync_Throws_If_A_Referenced_Permission_Is_Missing_From_The_Catalogue()
    {
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        IRolePermissionRepository rolePermissions = Substitute.For<IRolePermissionRepository>();
        ILogger<DefaultRoleCatalogueSeeder> logger = Substitute.For<ILogger<DefaultRoleCatalogueSeeder>>();

        // Missing "audit.view", which Auditor requires.
        permissions.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(BuildCatalogue(FullCatalogueNames.Where(n => n != "audit.view")));

        DefaultRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);

        Func<Task> act = () => seeder.SeedAsync(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*audit.view*");
    }
}
