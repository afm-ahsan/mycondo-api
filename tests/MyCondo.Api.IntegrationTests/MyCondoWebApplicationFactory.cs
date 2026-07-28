using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Boots the real host for HTTP-level tests. Only supplies the configuration required for the app
/// to pass its <c>ValidateOnStart</c> options checks (currently: a JWT signing key, which has no
/// default and isn't present in appsettings.json) — everything else uses the app's normal
/// Development configuration. Does not require a live PostgreSQL/Redis connection because nothing in
/// the startup path (as of Wave 0.5) eagerly resolves the DbContext or the Redis multiplexer.
/// </summary>
public sealed class MyCondoWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-only-signing-key-not-for-any-real-environment",
            });
        });
    }
}
