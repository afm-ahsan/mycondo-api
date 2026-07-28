using Microsoft.EntityFrameworkCore;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Interceptors;
using Npgsql;
using Testcontainers.PostgreSql;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Starts a real, ephemeral PostgreSQL container and runs migrations once, so tests can construct a
/// MyCondoDbContext bound to whichever tenant they want to act as — this is the most direct way to
/// exercise RLS itself, independent of the HTTP surface (see MyCondo.Api.IntegrationTests for that).
///
/// Two roles, mirroring docker-compose.yml/db/init/01_create_app_role.sql: Testcontainers' bootstrap
/// role (<c>mycondo_migrator</c> here) is always a Postgres superuser, which unconditionally bypasses
/// Row-Level Security regardless of FORCE — so migrations run as that role, but every
/// <see cref="CreateDbContext"/> the tests actually assert against runs as the separate, restricted
/// <c>mycondo_app</c> role. Using the bootstrap role for both (the original version of this fixture
/// did) makes RLS tests pass without RLS doing anything — exactly what happened until the first real
/// Testcontainers run here caught it. See the ADR recording that in mycondo-docs.
///
/// Requires a running Docker daemon.
/// </summary>
public sealed class MultiTenancyPostgresFixture : IAsyncLifetime
{
    private const string AppRolePassword = "mycondo_dev";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("mycondo_rls_test")
        .WithUsername("mycondo_migrator")
        .WithPassword("mycondo_migrator_test")
        .Build();

    private string _appConnectionString = string.Empty;

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

        await using MyCondoDbContext migrationContext = CreateMigratorDbContext();
        await migrationContext.Database.MigrateAsync();
    }

    /// <summary>Creates a DbContext acting as the given tenant (or no tenant, if null), connected as
    /// the restricted mycondo_app role — the same role the running API uses.</summary>
    public MyCondoDbContext CreateDbContext(Guid? tenantId)
    {
        TestTenantContextAccessor tenantAccessor = new() { CurrentTenantId = tenantId };

        DbContextOptions<MyCondoDbContext> options = new DbContextOptionsBuilder<MyCondoDbContext>()
            .UseNpgsql(_appConnectionString, npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new TenantContextConnectionInterceptor(tenantAccessor))
            .Options;

        return new MyCondoDbContext(options);
    }

    /// <summary>Owner/DDL role context, used only for running migrations in <see cref="InitializeAsync"/>.</summary>
    private MyCondoDbContext CreateMigratorDbContext()
    {
        DbContextOptions<MyCondoDbContext> options = new DbContextOptionsBuilder<MyCondoDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new MyCondoDbContext(options);
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
