using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Seed;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Verifies ArpDevelopmentBootstrapSeeder directly against a real, ephemeral PostgreSQL container —
/// needs a Docker daemon; not executed in the environment this was authored in (see PostgresApiFactory's
/// doc comment). The seeder is Development-only (see Program.cs) and this factory boots under "Testing",
/// so it's invoked explicitly here rather than relying on host startup — same pattern as
/// PlatformBootstrapSeederDbTests.
///
/// All RLS-protected reads (users/roles/role_assignments) go through
/// <see cref="PostgresApiFactory.CreateDbContextForTenant"/>, never the plain DI-resolved repositories
/// — those are bound to the API's request-scoped, HTTP-context-based ITenantContextAccessor, which has
/// no tenant to resolve outside a real request, so RLS correctly returns zero rows to them regardless
/// of what the seeder actually wrote (same reasoning as ArpDevelopmentBootstrapSeeder's own doc comment
/// for why ITS writes need a fixed-tenant context — the read side of this test class needs the exact
/// same fix, or every assertion here would silently observe an empty result set instead of the real
/// seeded data).
/// </summary>
public class ArpDevelopmentBootstrapSeederDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ArpDevelopmentBootstrapSeederDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task RunSeederAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ArpDevelopmentBootstrapSeeder seeder = new(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);
    }

    private async Task<Tenant> GetArpTenantAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        Tenant? arp = await tenants.GetBySlugAsync("arp", CancellationToken.None);
        arp.Should().NotBeNull();
        return arp!;
    }

    [Fact]
    public async Task Seeder_Provisions_Arp_Tenant_And_Admin_User()
    {
        await RunSeederAsync();

        Tenant arp = await GetArpTenantAsync();
        arp.Name.Should().Be("Akter Residence Park");
        arp.Status.Should().Be(TenantStatus.Active);

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(arp.Id.Value);
        bool adminExists = await db.Set<User>().AnyAsync(u => u.Email == "admin@mycondo.com");
        adminExists.Should().BeTrue();
    }

    [Fact]
    public async Task Seeder_Hashes_The_Admin_Password_Not_Plaintext()
    {
        await RunSeederAsync();

        Tenant arp = await GetArpTenantAsync();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(arp.Id.Value);
        User admin = await db.Set<User>().SingleAsync(u => u.Email == "admin@mycondo.com");

        admin.PasswordHash.Should().NotBe("Admin@1357#");

        using IServiceScope scope = _factory.Services.CreateScope();
        IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        hasher.Verify("Admin@1357#", admin.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Seeder_Assigns_OrganizationAdmin_TenantWide_Not_Legacy_SuperAdmin()
    {
        await RunSeederAsync();

        Tenant arp = await GetArpTenantAsync();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(arp.Id.Value);

        Role? organizationAdmin = await db.Set<Role>().SingleOrDefaultAsync(r => r.Name == "OrganizationAdmin");
        organizationAdmin.Should().NotBeNull();
        organizationAdmin!.IsSystem.Should().BeTrue();
        organizationAdmin.Code.Should().Be("organization.admin");
        organizationAdmin.RequiresBuildingScope.Should().BeFalse();

        Role? legacySuperAdmin = await db.Set<Role>().SingleOrDefaultAsync(r => r.Name == "SuperAdmin");
        legacySuperAdmin.Should().BeNull("ARP is bootstrapped fresh — it must never get the legacy tenant SuperAdmin role");

        User admin = await db.Set<User>().SingleAsync(u => u.Email == "admin@mycondo.com");
        List<RoleAssignment> assignments = await db.Set<RoleAssignment>()
            .Where(a => a.TenantId == arp.Id.Value && a.UserId == admin.Id)
            .ToListAsync();
        assignments.Should().ContainSingle(a => a.RoleId == organizationAdmin.Id && a.BuildingId == null);
    }

    [Fact]
    public async Task Seeder_Also_Seeds_Default_And_Condominium_Role_Catalogues()
    {
        await RunSeederAsync();

        Tenant arp = await GetArpTenantAsync();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(arp.Id.Value);
        List<Role> tenantRoles = await db.Set<Role>().Where(r => r.TenantId == arp.Id.Value).ToListAsync();

        tenantRoles.Select(r => r.Name).Should().BeEquivalentTo(
        [
            "OrganizationAdmin", "BuildingAdmin", "Treasurer", "Secretary", "SecurityHead", "Owner", "Renter", "Auditor",
            "CondoAdmin", "Manager", "Accountant", "SecurityOfficer", "FacilityManager",
            "FlatOwner", "Tenant",
        ]);
    }

    [Fact]
    public async Task Seeder_Is_Idempotent_No_Duplicates_On_Rerun()
    {
        await RunSeederAsync();
        await RunSeederAsync();

        // Tenant/admin creation is SlugExistsAsync-gated (a one-time bootstrap event); the role
        // catalogues reconcile by Code every run — either way, rerunning must not duplicate anything.
        Tenant arp = await GetArpTenantAsync();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(arp.Id.Value);
        List<Role> tenantRoles = await db.Set<Role>().Where(r => r.TenantId == arp.Id.Value).ToListAsync();
        tenantRoles.Should().HaveCount(15);
    }

    [Fact]
    public async Task Seeder_Also_Seeds_The_Default_Expense_Type_Catalogue()
    {
        await RunSeederAsync();

        Tenant arp = await GetArpTenantAsync();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(arp.Id.Value);
        List<ExpenseType> expenseTypes = await db.Set<ExpenseType>()
            .Where(e => e.TenantId == arp.Id.Value).ToListAsync();

        // Checks presence/shape rather than an exact set/all-active, since this class shares one
        // Postgres container across its tests (see class doc comment) and
        // Seeder_Preserves_A_Tenant_Deactivated_Expense_Type_On_Rerun deliberately deactivates one of
        // these rows on the same ARP tenant.
        expenseTypes.Select(e => e.Code).Should().Contain(
        [
            "CLEANING", "SECURITY", "GENFUEL", "LIFTMAINT", "PLUMBING", "ELECTRICAL",
            "PESTCTRL", "OFFICESUPPLY", "LEGALPROF", "REPAIRMAINT", "MISC",
        ]);
        expenseTypes.Should().ContainSingle(e => e.Code == "MISC" && e.IsActive);
    }

    [Fact]
    public async Task Seeder_Preserves_A_Tenant_Deactivated_Expense_Type_On_Rerun()
    {
        await RunSeederAsync();
        Tenant arp = await GetArpTenantAsync();

        await using (MyCondoDbContext db = _factory.CreateDbContextForTenant(arp.Id.Value))
        {
            ExpenseType cleaning = await db.Set<ExpenseType>()
                .SingleAsync(e => e.TenantId == arp.Id.Value && e.Code == "CLEANING");
            cleaning.Deactivate(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        await RunSeederAsync();

        await using MyCondoDbContext verifyDb = _factory.CreateDbContextForTenant(arp.Id.Value);
        List<ExpenseType> cleaningRows = await verifyDb.Set<ExpenseType>()
            .Where(e => e.TenantId == arp.Id.Value && e.Code == "CLEANING").ToListAsync();

        cleaningRows.Should().ContainSingle("reconciliation must never recreate a type the tenant deactivated");
        cleaningRows.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Seeder_Restores_A_Manually_Removed_Grant_On_Rerun_But_Preserves_Custom_Data()
    {
        await RunSeederAsync();
        Tenant arp = await GetArpTenantAsync();

        Guid removedPermissionId;
        Guid customRoleId;
        await using (MyCondoDbContext db = _factory.CreateDbContextForTenant(arp.Id.Value))
        {
            Role buildingAdmin = await db.Set<Role>()
                .SingleAsync(r => r.TenantId == arp.Id.Value && r.Code == "default.building-admin");
            RolePermission grant = await db.Set<RolePermission>()
                .FirstAsync(rp => rp.RoleId == buildingAdmin.Id);
            removedPermissionId = grant.PermissionId.Value;
            db.Set<RolePermission>().Remove(grant);

            // A hand-inserted role outside any catalogue — reconciliation must never touch this.
            Role custom = Role.CreateCustom(arp.Id.Value, "OnCallJanitor", "Ad-hoc tenant-created role.", DateTimeOffset.UtcNow);
            db.Set<Role>().Add(custom);
            customRoleId = custom.Id.Value;

            await db.SaveChangesAsync(CancellationToken.None);
        }

        await RunSeederAsync();

        await using MyCondoDbContext verifyDb = _factory.CreateDbContextForTenant(arp.Id.Value);
        Role buildingAdminAfter = await verifyDb.Set<Role>()
            .SingleAsync(r => r.TenantId == arp.Id.Value && r.Code == "default.building-admin");
        bool grantRestored = await verifyDb.Set<RolePermission>()
            .AnyAsync(rp => rp.RoleId == buildingAdminAfter.Id
                && rp.PermissionId == new PermissionId(removedPermissionId));
        grantRestored.Should().BeTrue("reconciliation must re-add a grant the catalogue still expects");

        bool customRoleSurvived = await verifyDb.Set<Role>().AnyAsync(r => r.Id == new RoleId(customRoleId));
        customRoleSurvived.Should().BeTrue("reconciliation must never remove a role outside the catalogue");

        List<Role> tenantRoles = await verifyDb.Set<Role>().Where(r => r.TenantId == arp.Id.Value).ToListAsync();
        tenantRoles.Should().HaveCount(16, "15 catalogue roles plus the one hand-inserted custom role");
    }
}
