using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyCondo.Infrastructure.Persistence.Seeding.Extensions;

namespace MyCondo.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development-only entry point for the seeding subsystem — currently just the ARP bootstrap dataset
/// (see <see cref="DevelopmentSeedExtensions.SeedArpDevelopmentBootstrapAsync"/>). Registered only when
/// the host environment is Development (see Program.cs), and deliberately registered *before*
/// <c>DevelopmentTenantSeeder</c> there: once this seeder has provisioned the ARP tenant,
/// <c>DevelopmentTenantSeeder</c>'s "if any tenant exists, do nothing" check no-ops, so the two
/// coexist without creating a redundant "demo" tenant. System/reference data and the authorization
/// (permission) catalogue are intentionally out of scope here — they're already seeded by migrations
/// (see the "Seed_*_Permission*" migrations) and that pattern is preserved as-is for already-published
/// history; new permissions going forward should get their own extension under
/// <c>Persistence/Seeding/Extensions</c> rather than a new migration.
/// </summary>
public sealed class DatabaseSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseSeeder> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.SeedArpDevelopmentBootstrapAsync(logger, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
