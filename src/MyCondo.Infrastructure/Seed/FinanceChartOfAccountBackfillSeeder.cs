using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Seed;

/// <summary>
/// Reconciles every existing tenant's Finance chart of accounts/mappings against
/// <see cref="FinanceChartOfAccountSeeder"/>'s current account set — the safeguard the
/// Billing↔Finance integration template calls for (its §4.1): a tenant whose first-user bootstrap ran
/// before the Finance foundation existed (or before this template added the
/// ServiceChargeIncome/GasRecoveryIncome/FineIncome/ResidentAdvance roles) never got those rows
/// created, since <c>RegisterUserCommandHandler</c> only invokes the seeder on first-user-of-tenant
/// bootstrap, not on every login. Left unfixed, that tenant's next Billing/Payment posting would fail
/// with <c>MissingAccountMappingException</c>.
///
/// Runs in every environment (not just Development, unlike <see cref="ArpDevelopmentBootstrapSeeder"/>/
/// <see cref="DevelopmentTenantSeeder"/>) — a Staging/Production tenant bootstrapped before Finance
/// existed has exactly the same gap. Safe to run unconditionally every app startup: the seeder is
/// itself idempotent (reconciles by <c>ChartOfAccount.Code</c>/<c>AccountMapping.PostingRole</c>,
/// inserts only what's missing), so a tenant that's already fully seeded is a no-op read-then-skip.
///
/// Deliberately does NOT resolve <c>IFinanceChartOfAccountSeeder</c> from this class's own DI scope —
/// that registration's <c>IChartOfAccountRepository</c>/<c>IAccountMappingRepository</c> are bound to
/// the shared, request-scoped <c>MyCondoDbContext</c>/<c>ITenantContextAccessor</c>, which has no
/// ambient tenant to resolve outside an HTTP request (same problem
/// <see cref="ArpDevelopmentBootstrapSeeder"/>'s doc comment describes). Instead this builds a fresh
/// <see cref="FinanceChartOfAccountSeeder"/> per tenant directly against a
/// <see cref="ITenantScopedUnitOfWork"/>'s fixed-tenant-context repositories — RLS stays fully
/// enforced; this only tells each write which tenant it's for, one tenant at a time.
/// </summary>
public sealed class FinanceChartOfAccountBackfillSeeder(
    IServiceScopeFactory scopeFactory,
    ITenantScopedUnitOfWorkFactory tenantScopedUnitOfWorkFactory,
    ILoggerFactory loggerFactory
)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ITenantRepository readOnlyTenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        List<Tenant> tenants = await readOnlyTenants.GetAllAsync(cancellationToken);
        DateTimeOffset nowUtc = clock.UtcNow;

        foreach (Tenant tenant in tenants)
        {
            await using ITenantScopedUnitOfWork tenantUow = tenantScopedUnitOfWorkFactory.Create(tenant.Id.Value);

            FinanceChartOfAccountSeeder seeder = new(
                tenantUow.ChartOfAccounts, tenantUow.AccountMappings,
                loggerFactory.CreateLogger<FinanceChartOfAccountSeeder>());
            await seeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);

            await tenantUow.SaveChangesAsync(cancellationToken);
        }

        loggerFactory.CreateLogger<FinanceChartOfAccountBackfillSeeder>().LogInformation(
            "[DatabaseSeed] Finance chart-of-accounts backfill: {TenantCount} tenant(s) reconciled",
            tenants.Count);
    }
}
