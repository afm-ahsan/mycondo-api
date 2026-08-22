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
    // Mirrors the 60 names actually referenced by the default role catalogue below — using the same set
    // here means a typo in DefaultRoleCatalogueSeeder's permission lists fails this test the same way
    // it would fail against the real catalogue.
    private static readonly string[] FullCatalogueNames =
    [
        "audit.view", "billing.fine.assess", "billing.fine.reverse", "billing.fine.view",
        "billing.fine.waive", "billing.generate", "billing.rule.manage", "billing.rule.view",
        "complaint.assign", "complaint.create", "complaint.manage", "complaint.view",
        "document.delete", "document.upload", "document.view", "expense.manage", "expense.view",
        "expense.approve", "expense.pay", "expensetype.manage", "expensetype.view",
        "expensecategory.view", "expensecategory.manage",
        "finance.bankaccount.view", "finance.bankaccount.manage", "finance.fixeddeposit.view",
        "finance.fixeddeposit.place", "finance.fixeddeposit.manage", "finance.fixeddeposit.interest.record",
        "finance.reconciliation.view", "finance.reconciliation.manage", "finance.reconciliation.reconcile",
        "invoice.view", "invoice.void", "lease.manage", "lease.view", "notification.manage",
        "notification.view", "ownership.manage", "ownership.view", "payment.record",
        "payment.reverse", "payment.view", "permission.view", "property.create", "property.delete",
        "property.update", "property.view", "report.financial.view", "report.operational.view",
        "resident.create", "resident.disable", "resident.update", "resident.view", "role.manage",
        "role.view", "tenant.manage", "tenant.view", "user.create", "user.disable", "user.update",
        "user.view", "workorder.assign", "workorder.complete", "workorder.create", "workorder.view",
        "security.directory.view",
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

        // A hand-maintained running total here breaks on every unrelated future permission addition —
        // Template 6 governance review replaced it with structural/semantic checks; per-role grant
        // *content* is covered by the dedicated tests below (e.g.
        // SeedAsync_Grants_Treasurer_The_Full_Fine_Correction_Authority_Permission_Set).
        addedGrants.Should().NotBeEmpty();
        addedGrants.Should().OnlyContain(g => g.TenantId == tenantId);
        addedGrants.Select(g => (g.RoleId, g.PermissionId)).Should().OnlyHaveUniqueItems(
            "no role should ever be granted the same permission twice");
    }

    [Fact]
    public async Task SeedAsync_Grants_Treasurer_The_Full_Fine_Correction_Authority_Permission_Set()
    {
        // Billing↔Finance integration closure item 1: Treasurer already has invoice.void/payment.reverse
        // (correction authority) — it must get the equivalent Fine permissions too.
        List<Permission> catalogue = BuildCatalogue(FullCatalogueNames);
        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions, ILogger<DefaultRoleCatalogueSeeder> logger) =
            BuildSubstitutes(catalogue);

        List<Role> addedRoles = [];
        roles.Add(Arg.Do<Role>(r => addedRoles.Add(r)));
        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        DefaultRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        Guid tenantId = Guid.NewGuid();
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        Role treasurer = addedRoles.Single(r => r.Name == "Treasurer");
        HashSet<PermissionId> treasurerGrantedPermissionIds = addedGrants
            .Where(g => g.RoleId == treasurer.Id).Select(g => g.PermissionId).ToHashSet();
        HashSet<string> treasurerGrantedNames = catalogue
            .Where(p => treasurerGrantedPermissionIds.Contains(p.Id)).Select(p => p.Name).ToHashSet();

        treasurerGrantedNames.Should().Contain(
            ["billing.fine.view", "billing.fine.assess", "billing.fine.waive", "billing.fine.reverse"]);
    }

    [Fact]
    public async Task SeedAsync_Grants_Treasurer_Full_Reconciliation_Authority_And_Auditor_View_Only()
    {
        List<Permission> catalogue = BuildCatalogue(FullCatalogueNames);
        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions, ILogger<DefaultRoleCatalogueSeeder> logger) =
            BuildSubstitutes(catalogue);

        List<Role> addedRoles = [];
        roles.Add(Arg.Do<Role>(r => addedRoles.Add(r)));
        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        DefaultRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        await seeder.SeedAsync(Guid.NewGuid(), DateTimeOffset.UtcNow, CancellationToken.None);

        HashSet<string> GrantedNamesFor(string roleName)
        {
            Role role = addedRoles.Single(r => r.Name == roleName);
            HashSet<PermissionId> ids = addedGrants.Where(g => g.RoleId == role.Id).Select(g => g.PermissionId).ToHashSet();
            return catalogue.Where(p => ids.Contains(p.Id)).Select(p => p.Name).ToHashSet();
        }

        GrantedNamesFor("Treasurer").Should().Contain(
            ["finance.reconciliation.view", "finance.reconciliation.manage", "finance.reconciliation.reconcile"]);
        GrantedNamesFor("Auditor").Should().Contain("finance.reconciliation.view");
        GrantedNamesFor("Auditor").Should().NotContain(["finance.reconciliation.manage", "finance.reconciliation.reconcile"],
            "completing/managing a reconciliation is correction-adjacent authority — Auditor stays read-only");
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
        // Every newly-created role starts with no grants; SecurityHead specifically already has both of
        // its expected grants ("complaint.view", "security.directory.view") persisted from a prior run.
        rolePermissions.GetForRoleAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns([]);
        Permission complaintView = catalogue.Single(p => p.Name == "complaint.view");
        Permission securityDirectoryView = catalogue.Single(p => p.Name == "security.directory.view");
        rolePermissions.GetForRoleAsync(existingSecurityHead.Id, Arg.Any<CancellationToken>())
            .Returns([
                new RolePermission(tenantId, existingSecurityHead.Id, complaintView.Id, DateTimeOffset.UtcNow, null),
                new RolePermission(tenantId, existingSecurityHead.Id, securityDirectoryView.Id, DateTimeOffset.UtcNow, null),
            ]);

        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        DefaultRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        // The other six default roles are newly created here (no existing rows for them), so they get
        // their full grant sets added — the point of this test is specifically that SecurityHead,
        // which already had both of its expected grants, gets nothing extra.
        addedGrants.Should().NotContain(g => g.RoleId == existingSecurityHead.Id);
    }

    [Fact]
    public async Task SeedAsync_Backfills_Code_On_A_Legacy_Code_Less_Role_Instead_Of_Duplicating_It()
    {
        // Reproduces the bug found running this seeder against a real, pre-existing tenant bootstrapped
        // before Role.CreateCustom accepted a code: "BuildingAdmin" exists with Code == null. Without
        // the Name-based fallback, SeedAsync would try to create a second "BuildingAdmin" role and the
        // database's (tenant_id, name) unique constraint would reject it.
        Guid tenantId = Guid.NewGuid();
        List<Permission> catalogue = BuildCatalogue(FullCatalogueNames);
        Role legacyBuildingAdmin = Role.CreateCustom(tenantId, "BuildingAdmin", "desc", DateTimeOffset.UtcNow);
        legacyBuildingAdmin.Code.Should().BeNull("this reproduces a role created before Code existed");

        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions, ILogger<DefaultRoleCatalogueSeeder> logger) =
            BuildSubstitutes(catalogue, [legacyBuildingAdmin]);

        List<Role> addedRoles = [];
        roles.Add(Arg.Do<Role>(r => addedRoles.Add(r)));
        List<RolePermission> addedGrants = [];
        rolePermissions.Add(Arg.Do<RolePermission>(g => addedGrants.Add(g)));

        DefaultRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        addedRoles.Should().NotContain(r => r.Name == "BuildingAdmin", "the legacy row must be adopted, not duplicated");
        legacyBuildingAdmin.Code.Should().Be("default.building-admin", "the legacy row's Code must be backfilled from the catalogue");
        addedGrants.Should().Contain(g => g.RoleId == legacyBuildingAdmin.Id, "grants still get reconciled onto the backfilled role");
    }

    [Fact]
    public async Task SeedAsync_Does_Not_Backfill_A_Role_That_Already_Has_A_Different_Code()
    {
        Guid tenantId = Guid.NewGuid();
        List<Permission> catalogue = BuildCatalogue(FullCatalogueNames);
        // A role that already has some other Code should never be touched by name-matching — only
        // genuinely code-less legacy rows are eligible for backfill.
        Role unrelatedCodedRole = Role.CreateSystem(
            RoleId.New(), tenantId, "BuildingAdmin", "desc", DateTimeOffset.UtcNow, code: "some.other.code");

        (IRoleRepository roles, IPermissionRepository permissions, IRolePermissionRepository rolePermissions, ILogger<DefaultRoleCatalogueSeeder> logger) =
            BuildSubstitutes(catalogue, [unrelatedCodedRole]);

        List<Role> addedRoles = [];
        roles.Add(Arg.Do<Role>(r => addedRoles.Add(r)));

        DefaultRoleCatalogueSeeder seeder = new(roles, permissions, rolePermissions, logger);
        await seeder.SeedAsync(tenantId, DateTimeOffset.UtcNow, CancellationToken.None);

        unrelatedCodedRole.Code.Should().Be("some.other.code");
        addedRoles.Should().Contain(r => r.Code == "default.building-admin",
            "the catalogue's BuildingAdmin entry gets its own new role since the existing 'BuildingAdmin'-named role belongs to a different Code");
    }
}
