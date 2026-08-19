using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Infrastructure.Seed;

/// <summary>
/// The single, explicit seed-orchestration entry point — <c>await app.Services.SeedDatabaseAsync(app.Environment)</c>
/// in <c>Program.cs</c>. Order is deliberate and auditable, not implicit hosted-service registration
/// order: (1) the global permission catalogue, required in every environment since every later step
/// resolves permission names against it; (2) Development-only bootstrap/demo seeders, behind a hard
/// runtime <c>IHostEnvironment.IsDevelopment()</c> check — not just DI-registration gating — so this
/// can never run in Production even if a future change accidentally registers those seeders
/// unconditionally.
///
/// Wrapped in a Postgres session-level advisory lock so multiple API instances starting concurrently
/// (a future deployment concern, not true of local dev today) serialize through this method rather
/// than racing each other — every seeder underneath is independently safe under "insert missing,
/// preserve everything else" reconciliation, but the lock avoids redundant work and any window where
/// two instances both observe "permission X is missing" and both attempt to insert it.
/// <c>MyCondo.DbMigrator</c>'s tenant-bootstrap CLI command is a single-operator invocation, not a
/// multi-instance concern, and does not need this lock — see its own "refuse if any tenant exists"
/// guard instead.
///
/// Skips entirely under the <c>"Testing"</c> environment — the established convention
/// (<c>MyCondoWebApplicationFactory</c>'s own doc comment) for HTTP-level tests that boot the real
/// host but deliberately have no live PostgreSQL connection, precisely so nothing in the startup path
/// eagerly requires a database. "Every environment" for permission seeding means every real deployment
/// environment (Development/Staging/Production), not this test-double one.
/// </summary>
public static class DatabaseSeederExtensions
{
    // A fixed, arbitrary 32-bit key scoped to this one advisory lock's purpose. pg_advisory_lock takes
    // bigint; an int argument is implicitly widened, so this is unambiguous and doesn't collide with
    // some other unrelated feature picking its own small integer.
    private const int SeedAdvisoryLockKey = 875_142_001;

    public static async Task SeedDatabaseAsync(
        this IServiceProvider services, IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return;
        }

        using IServiceScope scope = services.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;
        ILogger logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("MyCondo.DatabaseSeed");

        MyCondoDbContext lockContext = sp.GetRequiredService<MyCondoDbContext>();
        await lockContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await lockContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_lock({SeedAdvisoryLockKey})", cancellationToken);

            // 01 Permissions — system catalogue, every environment, every subsequent step depends on it.
            await sp.GetRequiredService<IPermissionSeeder>().SeedAsync(cancellationToken);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

            if (environment.IsDevelopment())
            {
                // 02 Platform bootstrap (Platform SuperAdmin — no tenant concept involved).
                await sp.GetRequiredService<PlatformBootstrapSeeder>().SeedAsync(cancellationToken);

                // 03 Development/demo tenant provisioning — ARP first so it's the canonical local-dev
                // tenant, then the generic "demo" tenant only if ARP didn't already claim the "any
                // tenant exists" slot DevelopmentTenantSeeder checks. Neither seeder goes through
                // RegisterUserCommandHandler for its admin user (they write identity rows directly),
                // so neither triggers Finance chart-of-account seeding on its own — step 04 below
                // covers that in the same startup run.
                await sp.GetRequiredService<ArpDevelopmentBootstrapSeeder>().SeedAsync(cancellationToken);
                await sp.GetRequiredService<DevelopmentTenantSeeder>().SeedAsync(cancellationToken);
            }
            else
            {
                logger.LogInformation(
                    "[DatabaseSeed] Skipping Development-only bootstrap/demo seeders — environment is {EnvironmentName}",
                    environment.EnvironmentName);
            }

            // 04 Tenant role/permission catalogue backfill — every environment, not Development-only:
            // a permission added to the catalogue (or a role's static permission list updated) after a
            // tenant's first-user bootstrap never reaches that tenant's roles otherwise — reconciles
            // OrganizationAdmin's blanket grant and the three role-catalogue seeders for every tenant.
            // See TenantRoleCatalogueBackfillSeeder's doc comment.
            await sp.GetRequiredService<TenantRoleCatalogueBackfillSeeder>().SeedAsync(cancellationToken);

            // 05 Finance chart-of-accounts backfill — every environment, not Development-only: a real
            // Staging/Production tenant bootstrapped before the Finance foundation (or before a later
            // Finance role was added) has the exact same missing-mapping gap a dev tenant would. Runs
            // after step 03 so a tenant created there (e.g. ARP) is already visible to it in the same
            // startup, not just from the next restart onward. See
            // FinanceChartOfAccountBackfillSeeder's doc comment.
            await sp.GetRequiredService<FinanceChartOfAccountBackfillSeeder>().SeedAsync(cancellationToken);

            // 06 Expense category catalogue backfill (Template 3) — every environment: a tenant
            // bootstrapped before Template 3 introduced Expense Categories has expense types with no
            // category and none of the default categories. Includes the OperatingExpense/AccountsPayable
            // system accounts via step 05 above (FinanceChartOfAccountSeeder's set), and this step's own
            // category/type reconciliation. See ExpenseCategoryCatalogueBackfillSeeder's doc comment.
            await sp.GetRequiredService<ExpenseCategoryCatalogueBackfillSeeder>().SeedAsync(cancellationToken);
        }
        finally
        {
            await lockContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_unlock({SeedAdvisoryLockKey})", CancellationToken.None);
            await lockContext.Database.CloseConnectionAsync();
        }
    }
}
