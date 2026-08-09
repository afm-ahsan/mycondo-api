using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
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
        await seeder.StartAsync(CancellationToken.None);
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
        ]);
    }

    [Fact]
    public async Task Seeder_Is_Idempotent()
    {
        await RunSeederAsync();
        await RunSeederAsync();

        // SlugExistsAsync-gated seeder: a second run is a full no-op, so re-fetching by slug still
        // resolves to exactly the one tenant created the first time — no duplicate-key violation.
        Tenant arp = await GetArpTenantAsync();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(arp.Id.Value);
        List<Role> tenantRoles = await db.Set<Role>().Where(r => r.TenantId == arp.Id.Value).ToListAsync();
        tenantRoles.Should().HaveCount(13);
    }
}
