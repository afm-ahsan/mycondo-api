using Microsoft.EntityFrameworkCore;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Interceptors;
using Npgsql;
using Testcontainers.PostgreSql;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Starts a real PostgreSQL instance and runs migrations once, so tests can construct a
/// MyCondoDbContext bound to whichever tenant they want to act as — this is the most direct way to
/// exercise RLS itself, independent of the HTTP surface (see MyCondo.Api.IntegrationTests for that).
///
/// Two roles, mirroring docker-compose.yml/db/init/01_create_app_role.sql: the migrator role is
/// DDL/owner-capable, but every <see cref="CreateDbContext"/> the tests actually assert against runs
/// as the separate, restricted <c>mycondo_app</c> role. Using the same role for both (the original
/// version of this fixture did) makes RLS tests pass without RLS doing anything — exactly what
/// happened until the first real Testcontainers run here caught it. See the ADR recording that in
/// mycondo-docs.
///
/// Two supported paths, selected by the presence of <see cref="ExternalConnectionEnvVar"/> — see
/// PostgresApiFactory's doc comment for the full rationale (identical pattern, kept in sync
/// deliberately rather than sharing a base class, since these two fixtures already didn't share one).
///
/// - Default: starts a real, ephemeral PostgreSQL container via Testcontainers (bootstrap role
///   <c>mycondo_migrator</c> is always a Postgres superuser, which unconditionally bypasses RLS
///   regardless of FORCE). Requires a running Docker daemon. Unchanged from before this addition.
/// - External/native PostgreSQL (test-only — see
///   mycondo-phase1-final-postgresql-rls-verification-prompt.md): skips Testcontainers, migrates an
///   already-isolated, disposable database directly. Never creates roles/databases itself.
/// </summary>
public sealed class MultiTenancyPostgresFixture : IAsyncLifetime
{
    private const string AppRolePassword = "mycondo_dev";
    private const string ExternalConnectionEnvVar = "MYCONDO_TEST_EXTERNAL_POSTGRES_CONNECTION";
    private const string ExternalAppPasswordEnvVar = "MYCONDO_TEST_EXTERNAL_APP_PASSWORD";

    // Deliberately NOT constructed here: PostgreSqlBuilder.Build() eagerly probes for a Docker
    // endpoint (see DockerEndpointAuthenticationProvider.IsAvailable), which would throw during this
    // fixture's own construction — before InitializeAsync even runs — on any machine without Docker,
    // regardless of whether the external-Postgres path was about to be used instead. Built lazily,
    // only inside the Testcontainers branch below.
    private PostgreSqlContainer? _postgres;

    private string _appConnectionString = string.Empty;
    private string _migratorConnectionStringForMigrationOnly = string.Empty;
    private bool _usingExternalPostgres;

    public async Task InitializeAsync()
    {
        string? externalMigratorConnectionString = Environment.GetEnvironmentVariable(ExternalConnectionEnvVar);
        string migratorConnectionString;
        string appRolePassword;

        if (externalMigratorConnectionString is not null)
        {
            _usingExternalPostgres = true;
            migratorConnectionString = externalMigratorConnectionString;
            appRolePassword = Environment.GetEnvironmentVariable(ExternalAppPasswordEnvVar) ?? AppRolePassword;
            // No role/database creation here by design — see this class's doc comment.
        }
        else
        {
            _postgres = new PostgreSqlBuilder("postgres:18-alpine")
                .WithDatabase("mycondo_rls_test")
                .WithUsername("mycondo_migrator")
                .WithPassword("mycondo_migrator_test")
                .Build();

            await _postgres.StartAsync();
            migratorConnectionString = _postgres.GetConnectionString();
            appRolePassword = AppRolePassword;

            await using NpgsqlConnection bootstrapConnection = new(migratorConnectionString);
            await bootstrapConnection.OpenAsync();
            await using NpgsqlCommand createAppRole = bootstrapConnection.CreateCommand();
            createAppRole.CommandText =
                $"""
                CREATE ROLE mycondo_app WITH LOGIN PASSWORD '{AppRolePassword}'
                  NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
                """;
            await createAppRole.ExecuteNonQueryAsync();
        }

        _migratorConnectionStringForMigrationOnly = migratorConnectionString;

        NpgsqlConnectionStringBuilder appConnectionBuilder = new(migratorConnectionString)
        {
            Username = "mycondo_app",
            Password = appRolePassword,
        };
        _appConnectionString = appConnectionBuilder.ConnectionString;

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
            .UseNpgsql(_migratorConnectionStringForMigrationOnly, npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new MyCondoDbContext(options);
    }

    public async Task DisposeAsync()
    {
        // Never created in the external-Postgres path — see InitializeAsync. The external
        // verification database is intentionally left for the caller to clean up once, at the end of
        // a whole verification pass, not per test-fixture instance.
        if (!_usingExternalPostgres && _postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }
}
