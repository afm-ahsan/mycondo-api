using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Seed;

/// <summary>
/// Reconciles every existing tenant's Expense Category catalogue against
/// <see cref="ExpenseCategoryCatalogueSeeder"/>'s current default set, and backfills any pre-Template-3
/// <c>ExpenseType</c> row's null <c>ExpenseCategoryId</c> — the same "a tenant bootstrapped before this
/// template existed never got these rows" gap <see cref="FinanceChartOfAccountBackfillSeeder"/> exists
/// to close for the Finance chart of accounts (see that class's doc comment for the full rationale, which
/// applies identically here). Runs in every environment, unconditionally, every app startup — the
/// underlying seeder is itself idempotent.
/// </summary>
public sealed class ExpenseCategoryCatalogueBackfillSeeder(
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

            ExpenseCategoryCatalogueSeeder categorySeeder = new(
                tenantUow.ExpenseCategories, tenantUow.ExpenseTypes,
                loggerFactory.CreateLogger<ExpenseCategoryCatalogueSeeder>());
            await categorySeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
            await tenantUow.SaveChangesAsync(cancellationToken);

            // A second pass, after the category rows above are saved, so any newly-created default
            // ExpenseType this tenant is still missing has a category to attach to.
            ExpenseTypeCatalogueSeeder typeSeeder = new(
                tenantUow.ExpenseTypes, tenantUow.ExpenseCategories,
                loggerFactory.CreateLogger<ExpenseTypeCatalogueSeeder>());
            await typeSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
            await tenantUow.SaveChangesAsync(cancellationToken);
        }

        loggerFactory.CreateLogger<ExpenseCategoryCatalogueBackfillSeeder>().LogInformation(
            "[DatabaseSeed] Expense category catalogue backfill: {TenantCount} tenant(s) reconciled",
            tenants.Count);
    }
}
