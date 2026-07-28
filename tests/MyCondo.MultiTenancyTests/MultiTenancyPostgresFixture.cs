using Microsoft.EntityFrameworkCore;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Interceptors;
using Testcontainers.PostgreSql;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Starts a real, ephemeral PostgreSQL container and runs migrations once, so tests can construct a
/// MyCondoDbContext bound to whichever tenant they want to act as — this is the most direct way to
/// exercise RLS itself, independent of the HTTP surface (see MyCondo.Api.IntegrationTests for that).
///
/// Requires a running Docker daemon. Written and reviewed for correctness but NOT executed in the
/// environment it was authored in (Docker Desktop's backend was unavailable there) — run wherever
/// Docker is actually available before trusting it blindly.
/// </summary>
public sealed class MultiTenancyPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("mycondo_rls_test")
        .WithUsername("mycondo_test")
        .WithPassword("mycondo_test")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using MyCondoDbContext migrationContext = CreateDbContext(tenantId: null);
        await migrationContext.Database.MigrateAsync();
    }

    /// <summary>Creates a DbContext acting as the given tenant (or no tenant, if null).</summary>
    public MyCondoDbContext CreateDbContext(Guid? tenantId)
    {
        TestTenantContextAccessor tenantAccessor = new() { CurrentTenantId = tenantId };

        DbContextOptions<MyCondoDbContext> options = new DbContextOptionsBuilder<MyCondoDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new TenantContextConnectionInterceptor(tenantAccessor))
            .Options;

        return new MyCondoDbContext(options);
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
