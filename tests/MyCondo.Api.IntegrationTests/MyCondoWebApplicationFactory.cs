using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Boots the real host for HTTP-level tests. Only supplies the configuration required for the app
/// to pass its <c>ValidateOnStart</c> options checks (currently: a JWT signing key, which has no
/// default and isn't present in appsettings.json) — everything else uses the app's normal
/// configuration. Does not require a live PostgreSQL/Redis connection because nothing in the startup
/// path (as of this slice) eagerly resolves the DbContext or the Redis multiplexer.
///
/// Uses the "Testing" environment (not "Development", which <see cref="WebApplicationFactory{TEntryPoint}"/>
/// would otherwise default to) specifically so <c>DevelopmentTenantSeeder</c> — a Development-only
/// hosted service that hits the database on startup — does not run here. Tests that need a seeded
/// tenant seed it explicitly against a real database (see the Testcontainers-based tests), not via
/// that seeder.
/// </summary>
public sealed class MyCondoWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-only-signing-key-not-for-any-real-environment",
            });
        });
    }
}
