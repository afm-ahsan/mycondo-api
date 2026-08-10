using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Services;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Interceptors;
using MyCondo.Infrastructure.Persistence.Repositories;
using Npgsql;
using Testcontainers.PostgreSql;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Boots the real host against a real PostgreSQL instance and runs migrations before tests execute —
/// for the round-trip flows that genuinely need a database (register/login/etc.), as opposed to
/// <see cref="MyCondoWebApplicationFactory"/>'s no-DB tests.
///
/// Two roles, mirroring docker-compose.yml/db/init/01_create_app_role.sql: the migrator role is
/// DDL/owner-capable, but the actual app-under-test (everything <see cref="Services"/> resolves, i.e.
/// every HTTP call a test makes) must run as a separate, restricted <c>mycondo_app</c> role or these
/// tests would "pass" without RLS meaning anything. This gap is exactly what let RLS silently do
/// nothing in every environment until the first real Testcontainers run caught it — see the ADR
/// recording that in mycondo-docs.
///
/// Two supported paths, selected by the presence of <see cref="ExternalConnectionEnvVar"/>:
///
/// - Default (CI, most local runs): starts a real, ephemeral PostgreSQL container via Testcontainers.
///   Its bootstrap role (<c>mycondo_migrator</c> here) is always a Postgres superuser (Testcontainers'
///   own bootstrap-user convention), which unconditionally bypasses RLS — this path creates a fresh
///   <c>mycondo_app</c> role itself, since the container starts empty. Requires a running Docker
///   daemon. Behavior here is completely unchanged from before this external-path addition.
///
/// - External/native PostgreSQL (test-only — see
///   mycondo-phase1-final-postgresql-rls-verification-prompt.md): when
///   <see cref="ExternalConnectionEnvVar"/> is set to a migrator-capable connection string pointing at
///   an already-isolated, disposable verification database, this path skips Testcontainers entirely
///   and migrates that database directly. It never creates roles or databases itself — the target is
///   expected to be pre-prepared (an isolated, disposable database, with the literal `mycondo_app` role
///   already existing, since migrations GRANT to that literal name) — this keeps the adaptation
///   test-only and introduces no machine-specific assumption into production code or the default path.
/// </summary>
public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string AppRolePassword = "mycondo_dev";

    /// <summary>Migrator-capable connection string to an already-isolated, disposable verification
    /// database. When set, Testcontainers is not used at all. Test-only — never read by production
    /// code.</summary>
    private const string ExternalConnectionEnvVar = "MYCONDO_TEST_EXTERNAL_POSTGRES_CONNECTION";

    /// <summary>Password for the pre-existing `mycondo_app` role on the external instance. Defaults to
    /// the same value the Testcontainers path uses if not overridden.</summary>
    private const string ExternalAppPasswordEnvVar = "MYCONDO_TEST_EXTERNAL_APP_PASSWORD";

    // Deliberately NOT constructed here: PostgreSqlBuilder.Build() eagerly probes for a Docker
    // endpoint (see DockerEndpointAuthenticationProvider.IsAvailable), which would throw during this
    // factory's own construction — before InitializeAsync even runs — on any machine without Docker,
    // regardless of whether the external-Postgres path was about to be used instead. Built lazily,
    // only inside the Testcontainers branch below.
    private PostgreSqlContainer? _postgres;

    private string _appConnectionString = string.Empty;
    private bool _usingExternalPostgres;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-only-signing-key-not-for-any-real-environment",
                ["ConnectionStrings:Default"] = _appConnectionString,

                // The "auth"/"platform-auth" rate-limit policies partition by client IP, but every
                // request from this in-memory TestServer shares one partition ("anon") across every
                // test method in a *DbTests class (IClassFixture reuses one host per class) — the
                // production-appropriate 10/minute and 5/minute limits would otherwise 429 unrelated
                // authorization tests once enough Register/Login calls accumulate. Raised here, not in
                // any appsettings.json, so production/Development keep the real brute-force bound.
                ["RateLimiting:Auth:PermitLimit"] = "100000",
                ["RateLimiting:Auth:WindowSeconds"] = "1",
                ["RateLimiting:PlatformAuth:PermitLimit"] = "100000",
                ["RateLimiting:PlatformAuth:WindowSeconds"] = "1",
            });
        });
    }

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
                .WithDatabase("mycondo_test")
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

        NpgsqlConnectionStringBuilder appConnectionBuilder = new(migratorConnectionString)
        {
            Username = "mycondo_app",
            Password = appRolePassword,
        };
        _appConnectionString = appConnectionBuilder.ConnectionString;

        // Migrate as the migrator role (owner/DDL) via a context built directly against the migrator
        // connection string — not through Services/DI, which (via ConfigureWebHost above) will only
        // ever see the restricted _appConnectionString once it's built.
        DbContextOptions<MyCondoDbContext> migratorOptions = new DbContextOptionsBuilder<MyCondoDbContext>()
            .UseNpgsql(migratorConnectionString, npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using (MyCondoDbContext migrationContext = new(migratorOptions))
        {
            await migrationContext.Database.MigrateAsync();

            // The global permission catalogue is no longer migration-seeded (see mycondo-docs ADR-022/
            // ADR-024 — moved to PermissionCatalogue/PermissionSeeder, reconciled at app startup by
            // DatabaseSeederExtensions.SeedDatabaseAsync, which Program.cs skips under the "Testing"
            // environment this factory boots under). Every test here that registers/logs in or exercises
            // role/permission endpoints needs identity.permissions populated, so this factory seeds it
            // explicitly — the same catalogue, reconciled the same way, just invoked directly instead of
            // through the environment-gated orchestrator. Reuses the already-open migrator connection;
            // identity.permissions has no RLS, so which role does this is immaterial.
            PermissionRepository permissionRepository = new(migrationContext);
            PermissionSeeder permissionSeeder = new(permissionRepository, NullLogger<PermissionSeeder>.Instance);
            await permissionSeeder.SeedAsync(CancellationToken.None);
            await migrationContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// A <see cref="MyCondoDbContext"/> connected as the restricted <c>mycondo_app</c> role (same as
    /// the app-under-test) with a fixed tenant context — for tests that need to verify RLS-protected
    /// state directly (e.g. row counts) rather than through an HTTP round-trip. A DbContext resolved
    /// from <see cref="Services"/> outside a real HTTP request has no JWT/HttpContext for the real
    /// <c>TenantContextAccessor</c> to read, so it would see an empty tenant context and RLS would
    /// correctly hide every row — this exists to give such tests an explicit tenant to act as instead.
    /// </summary>
    public MyCondoDbContext CreateDbContextForTenant(Guid tenantId)
    {
        FixedTenantContextAccessor tenantAccessor = new(tenantId);

        DbContextOptions<MyCondoDbContext> options = new DbContextOptionsBuilder<MyCondoDbContext>()
            .UseNpgsql(_appConnectionString, npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", schema: "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new TenantContextConnectionInterceptor(tenantAccessor))
            .Options;

        return new MyCondoDbContext(options);
    }

    private sealed class FixedTenantContextAccessor(Guid tenantId) : ITenantContextAccessor
    {
        public Guid? CurrentTenantId => tenantId;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Never created in the external-Postgres path — nothing to tear down, and the external
        // verification database is intentionally left for the caller to clean up (see the
        // verification prompt's "clean up disposable verification resources" step, run once at the
        // end of a whole verification pass, not per test-fixture instance).
        if (!_usingExternalPostgres && _postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
        await base.DisposeAsync();
    }
}
