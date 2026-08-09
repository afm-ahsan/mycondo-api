using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Seed;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Verifies the orchestrator's own runtime guard (spec §10: Development-only seeders must never
/// accidentally execute in Production) directly against a real, ephemeral PostgreSQL container — needs
/// a Docker daemon; not executed in the environment this was authored in (see PostgresApiFactory's doc
/// comment). <see cref="PostgresApiFactory"/> boots its host under "Testing", but
/// <see cref="DatabaseSeederExtensions.SeedDatabaseAsync"/> takes the environment as an explicit
/// parameter — substituted here with a fake reporting "Production" so this test exercises the same
/// hard runtime check Program.cs relies on, independent of whatever the host itself booted as.
/// </summary>
public class DatabaseSeederExtensionsDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public DatabaseSeederExtensionsDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static StubHostEnvironment FakeEnvironment(string name) => new(name);

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "MyCondo.Api.IntegrationTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public async Task SeedDatabaseAsync_In_Production_Seeds_Permissions_But_Never_Runs_Development_Seeders()
    {
        // Compares before/after rather than assuming a pristine database — this class's other test
        // shares the same PostgresApiFactory/database (one container per test class, per xUnit's
        // IClassFixture), and xUnit does not guarantee method execution order within a class.
        using IServiceScope beforeScope = _factory.Services.CreateScope();
        IPlatformUserRepository platformUsersBefore = beforeScope.ServiceProvider.GetRequiredService<IPlatformUserRepository>();
        bool superAdminExistedBefore = await platformUsersBefore.GetByEmailAsync("sadmin@mycondo.com", CancellationToken.None) is not null;
        ITenantRepository tenantsBefore = beforeScope.ServiceProvider.GetRequiredService<ITenantRepository>();
        bool arpExistedBefore = await tenantsBefore.SlugExistsAsync("arp", CancellationToken.None);
        bool demoExistedBefore = await tenantsBefore.SlugExistsAsync("demo", CancellationToken.None);

        await _factory.Services.SeedDatabaseAsync(FakeEnvironment(Environments.Production), CancellationToken.None);

        using IServiceScope scope = _factory.Services.CreateScope();

        // System catalogue: must still be seeded in Production.
        MyCondoDbContext db = scope.ServiceProvider.GetRequiredService<MyCondoDbContext>();
        int permissionCount = await db.Set<Permission>().CountAsync();
        permissionCount.Should().BeGreaterThan(0);

        // Development-only bootstrap/demo data: a Production call must never create any of it, whether
        // or not it already existed from something else this database saw.
        IPlatformUserRepository platformUsers = scope.ServiceProvider.GetRequiredService<IPlatformUserRepository>();
        bool superAdminExistsAfter = await platformUsers.GetByEmailAsync("sadmin@mycondo.com", CancellationToken.None) is not null;
        superAdminExistsAfter.Should().Be(superAdminExistedBefore, "a Production call must never create the Platform SuperAdmin");

        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        (await tenants.SlugExistsAsync("arp", CancellationToken.None)).Should().Be(arpExistedBefore, "a Production call must never create the ARP dev tenant");
        (await tenants.SlugExistsAsync("demo", CancellationToken.None)).Should().Be(demoExistedBefore, "a Production call must never create the demo tenant");
    }

    [Fact]
    public async Task SeedDatabaseAsync_Is_Idempotent_Across_Repeated_Calls_In_The_Same_Environment()
    {
        await _factory.Services.SeedDatabaseAsync(FakeEnvironment(Environments.Development), CancellationToken.None);
        await _factory.Services.SeedDatabaseAsync(FakeEnvironment(Environments.Development), CancellationToken.None);

        using IServiceScope scope = _factory.Services.CreateScope();
        MyCondoDbContext db = scope.ServiceProvider.GetRequiredService<MyCondoDbContext>();

        int permissionCount = await db.Set<Permission>().CountAsync();
        int distinctPermissionNames = await db.Set<Permission>().Select(p => p.Name).Distinct().CountAsync();
        permissionCount.Should().Be(distinctPermissionNames, "no permission name should ever be duplicated across reseeds");

        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        (await tenants.SlugExistsAsync("arp", CancellationToken.None)).Should().BeTrue();
    }
}
