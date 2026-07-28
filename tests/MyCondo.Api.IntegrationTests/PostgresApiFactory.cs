using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Boots the real host against a real, ephemeral PostgreSQL container (via Testcontainers) and runs
/// migrations before tests execute — for the round-trip flows that genuinely need a database
/// (register/login/etc.), as opposed to <see cref="MyCondoWebApplicationFactory"/>'s no-DB tests.
///
/// Requires a running Docker daemon. This was written and reviewed for correctness but NOT executed
/// in the environment it was authored in (Docker Desktop's backend was unavailable there) — run it
/// wherever Docker is actually available (a dev machine or CI) before trusting it blindly.
/// </summary>
public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("mycondo_test")
        .WithUsername("mycondo_test")
        .WithPassword("mycondo_test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-only-signing-key-not-for-any-real-environment",
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Accessing Services triggers host build now that ConfigureWebHost can read the container's
        // real connection string.
        using IServiceScope scope = Services.CreateScope();
        MyCondoDbContext db = scope.ServiceProvider.GetRequiredService<MyCondoDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
