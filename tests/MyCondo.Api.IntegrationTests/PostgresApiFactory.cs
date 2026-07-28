using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyCondo.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Boots the real host against a real, ephemeral PostgreSQL container (via Testcontainers) and runs
/// migrations before tests execute — for the round-trip flows that genuinely need a database
/// (register/login/etc.), as opposed to <see cref="MyCondoWebApplicationFactory"/>'s no-DB tests.
///
/// Two roles, mirroring docker-compose.yml/db/init/01_create_app_role.sql: Testcontainers' bootstrap
/// role (<c>mycondo_migrator</c> here) is always a Postgres superuser, which unconditionally bypasses
/// Row-Level Security — so migrations run as that role, but the actual app-under-test (everything
/// <see cref="Services"/> resolves, i.e. every HTTP call a test makes) must run as a separate,
/// restricted <c>mycondo_app</c> role or these tests would "pass" without RLS meaning anything. This
/// gap is exactly what let RLS silently do nothing in every environment until the first real
/// Testcontainers run caught it — see the ADR recording that in mycondo-docs.
///
/// Requires a running Docker daemon.
/// </summary>
public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string AppRolePassword = "mycondo_dev";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("mycondo_test")
        .WithUsername("mycondo_migrator")
        .WithPassword("mycondo_migrator_test")
        .Build();

    private string _appConnectionString = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-only-signing-key-not-for-any-real-environment",
                ["ConnectionStrings:Default"] = _appConnectionString,
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        string migratorConnectionString = _postgres.GetConnectionString();

        NpgsqlConnectionStringBuilder appConnectionBuilder = new(migratorConnectionString)
        {
            Username = "mycondo_app",
            Password = AppRolePassword,
        };
        _appConnectionString = appConnectionBuilder.ConnectionString;

        await using (NpgsqlConnection bootstrapConnection = new(migratorConnectionString))
        {
            await bootstrapConnection.OpenAsync();
            await using NpgsqlCommand createAppRole = bootstrapConnection.CreateCommand();
            createAppRole.CommandText =
                $"""
                CREATE ROLE mycondo_app WITH LOGIN PASSWORD '{AppRolePassword}'
                  NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
                """;
            await createAppRole.ExecuteNonQueryAsync();
        }

        // Migrate as mycondo_migrator (owner/DDL role) via a context built directly against the
        // bootstrap connection string — not through Services/DI, which (via ConfigureWebHost above)
        // will only ever see the restricted _appConnectionString once it's built.
        DbContextOptions<MyCondoDbContext> migratorOptions = new DbContextOptionsBuilder<MyCondoDbContext>()
            .UseNpgsql(migratorConnectionString, npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using (MyCondoDbContext migrationContext = new(migratorOptions))
        {
            await migrationContext.Database.MigrateAsync();
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
