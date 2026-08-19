using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

/// <summary>
/// Focused on the Billing↔Finance integration closure item: Accountant must receive the
/// billing.fine.view/assess "operational subset" (not waive/reverse, which stays with the tenant-wide
/// Treasurer role — see DefaultRoleCatalogueSeederTests' equivalent Treasurer test). Not a full
/// re-test of every CondominiumRoles entry — see DefaultRoleCatalogueSeederTests for the exhaustive
/// role-creation/reconciliation coverage this seeder shares the same pattern with.
/// </summary>
public class CondominiumRoleCatalogueSeederTests
{
    // The full distinct set of permission names referenced across all 5 CondominiumRoles entries —
    // SeedAsync throws if any referenced permission is missing from the catalogue.
    private static readonly string[] FullCatalogueNames =
    [
        "property.view", "property.update", "resident.view", "resident.create", "resident.update",
        "ownership.view", "ownership.manage", "lease.view", "billing.rule.view", "billing.generate",
        "invoice.view", "payment.view", "payment.record", "expense.view", "expense.manage",
        "expense.approve", "expense.pay", "expensetype.view", "expensetype.manage", "expensecategory.view",
        "expensecategory.manage", "complaint.view", "complaint.create", "complaint.assign", "complaint.manage",
        "workorder.view", "workorder.create", "workorder.assign", "workorder.complete", "document.view",
        "document.upload", "notification.view", "visitor.override", "vehicle.override",
        "report.operational.view", "notification.manage", "report.financial.view",
        "billing.fine.view", "billing.fine.assess",
        "finance.bankaccount.view", "finance.fixeddeposit.view", "finance.fixeddeposit.interest.record",
        "visitor.view", "visitor.create", "visitor.checkin", "visitor.checkout", "visitor.block.manage",
        "vehicle.view", "vehicle.create", "vehicle.checkin", "vehicle.checkout", "vehicle.block.manage",
        "parcel.view", "parcel.receive", "parcel.handover", "parcel.notify", "parcel.escalate",
        "parcel.return", "parcel.update", "report.security.view",
        "facility.view", "facility.manage", "facility.booking.view", "facility.booking.approve",
        "facility.booking.cancel", "facility.booking.inspect", "pool.view", "pool.manage",
        "pool.checkin", "pool.checkout", "pool.incident.manage", "generator.view", "generator.manage",
        "generator.operation.manage", "generator.fuel.manage", "generator.maintenance.manage",
        "gascylinder.view", "gascylinder.stock.manage", "report.facility",
    ];

    private static List<Permission> BuildCatalogue(IEnumerable<string> names) =>
        names.Select(n => Permission.Create(PermissionId.New(), n, n, n.Split('.')[0])).ToList();

    [Fact]
    public async Task SeedAsync_Grants_Accountant_The_Fine_View_And_Assess_Permissions_Only()
    {
        List<Permission> catalogue = BuildCatalogue(FullCatalogueNames);
        IRoleRepository roles = Substitute.For<IRoleRepository>();
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        IRolePermissionRepository rolePermissions = Substitute.For<IRolePermissionRepository>();
        ILogger<CondominiumRoleCatalogueSeeder> logger = Substitute.For<ILogger<CondominiumRoleCatalogueSeeder>>();

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        rolePermissions.GetForRoleAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns([]);

        List<Role> addedRoles = [];
        roles.Add(Arg.Do<Role>(r => addedRoles.Add(r)));
        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        CondominiumRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        Guid tenantId = Guid.NewGuid();
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        Role accountant = addedRoles.Single(r => r.Name == "Accountant");
        accountant.RequiresBuildingScope.Should().BeTrue();
        HashSet<PermissionId> accountantGrantedIds = addedGrants
            .Where(g => g.RoleId == accountant.Id).Select(g => g.PermissionId).ToHashSet();
        HashSet<string> accountantGrantedNames = catalogue
            .Where(p => accountantGrantedIds.Contains(p.Id)).Select(p => p.Name).ToHashSet();

        accountantGrantedNames.Should().Contain(["billing.fine.view", "billing.fine.assess"]);
        accountantGrantedNames.Should().NotContain(["billing.fine.waive", "billing.fine.reverse"],
            "waive/reverse are correction authority — that stays with the tenant-wide Treasurer role");
    }

    [Fact]
    public async Task SeedAsync_Adds_Missing_Fine_Grants_To_An_Existing_Accountant_Role_Without_Duplicating()
    {
        // Reproduces the real reconciliation scenario: a tenant whose Accountant role was seeded before
        // this closure item added the Fine permissions to its definition.
        Guid tenantId = Guid.NewGuid();
        List<Permission> catalogue = BuildCatalogue(FullCatalogueNames);
        Role existingAccountant = Role.CreateSystem(
            RoleId.New(), tenantId, "Accountant", "desc", DateTimeOffset.UtcNow,
            code: "condominium.accountant", requiresBuildingScope: true);

        IRoleRepository roles = Substitute.For<IRoleRepository>();
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        IRolePermissionRepository rolePermissions = Substitute.For<IRolePermissionRepository>();
        ILogger<CondominiumRoleCatalogueSeeder> logger = Substitute.For<ILogger<CondominiumRoleCatalogueSeeder>>();

        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);
        roles.GetAllForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([existingAccountant]);

        // Simulate the pre-existing grants: everything Accountant had before this closure item, minus
        // the two new Fine permissions. Includes Template 3's expense.pay/expensetype.manage/
        // expensecategory.* additions and Template 4's finance.bankaccount.*/finance.fixeddeposit.*
        // additions so this Fine-focused test's assertion stays scoped to Fine grants.
        List<RolePermission> priorGrants = catalogue
            .Where(p => p.Name is "billing.rule.view" or "billing.generate" or "invoice.view" or "payment.view"
                or "payment.record" or "expense.view" or "expense.manage" or "expense.pay" or "expensetype.view"
                or "expensetype.manage" or "expensecategory.view" or "expensecategory.manage" or "report.financial.view"
                or "finance.bankaccount.view" or "finance.fixeddeposit.view" or "finance.fixeddeposit.interest.record")
            .Select(p => new RolePermission(tenantId, existingAccountant.Id, p.Id, DateTimeOffset.UtcNow, null))
            .ToList();
        rolePermissions.GetForRoleAsync(existingAccountant.Id, Arg.Any<CancellationToken>()).Returns(priorGrants);
        rolePermissions.GetForRoleAsync(Arg.Is<RoleId>(id => id != existingAccountant.Id), Arg.Any<CancellationToken>())
            .Returns([]);

        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        CondominiumRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        List<RolePermission> accountantNewGrants = addedGrants.Where(g => g.RoleId == existingAccountant.Id).ToList();
        HashSet<string> newlyGrantedNames = catalogue
            .Where(p => accountantNewGrants.Any(g => g.PermissionId == p.Id)).Select(p => p.Name).ToHashSet();

        newlyGrantedNames.Should().BeEquivalentTo(["billing.fine.view", "billing.fine.assess"]);
    }
}
