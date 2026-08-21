using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Seed;

/// <summary>
/// Reconciles every existing tenant's role/permission grants against the current catalogue —
/// the generalized fix for the gap the Billing↔Finance integration template's new
/// <c>billing.fine.*</c> permissions surfaced: <c>RegisterUserCommandHandler</c> only invokes
/// <see cref="IOrganizationAdminBootstrapper.BootstrapAsync"/> and the three role-catalogue seeders
/// (<see cref="DefaultRoleCatalogueSeeder"/>, <see cref="CondominiumRoleCatalogueSeeder"/>,
/// <see cref="ResidentRoleCatalogueSeeder"/>) on a tenant's first-user registration — never again — so
/// a permission added to the catalogue (or a role's static permission list updated) afterward never
/// reaches an already-bootstrapped tenant's roles. Not specific to Fine or to any one tenant: this
/// reconciles every tenant returned by <see cref="ITenantRepository.GetAllAsync"/>, the same way
/// <see cref="FinanceChartOfAccountBackfillSeeder"/> already does for the Finance chart of accounts.
///
/// Every underlying seeder/reconciler is additive-only by construction — grants missing permissions,
/// never revokes an existing grant or role (a tenant admin's own custom roles, or a permission grant no
/// catalogue definition lists, are left untouched) — so this is safe to run unconditionally on every
/// app startup, in every environment, same as <see cref="FinanceChartOfAccountBackfillSeeder"/>.
///
/// Deliberately does NOT resolve any of these services from this class's own DI scope — same reasoning
/// as <see cref="FinanceChartOfAccountBackfillSeeder"/>: their registered <c>IRoleRepository</c>/
/// <c>IPermissionRepository</c>/<c>IRolePermissionRepository</c>/<c>IRoleAssignmentRepository</c> are
/// bound to the shared, request-scoped <c>MyCondoDbContext</c>/<c>ITenantContextAccessor</c>, which has
/// no ambient tenant to resolve outside an HTTP request. Instead this builds fresh instances per tenant
/// directly against a <see cref="ITenantScopedUnitOfWork"/>'s fixed-tenant-context repositories — RLS
/// stays fully enforced; this only tells each write which tenant it's for, one tenant at a time, so no
/// write can ever cross tenant boundaries.
/// </summary>
public sealed class TenantRoleCatalogueBackfillSeeder(
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

        int organizationAdminGrantsCreated = 0;

        foreach (Tenant tenant in tenants)
        {
            await using ITenantScopedUnitOfWork tenantUow = tenantScopedUnitOfWorkFactory.Create(tenant.Id.Value);

            OrganizationAdminBootstrapper organizationAdminBootstrapper = new(
                tenantUow.Roles, tenantUow.Permissions, tenantUow.RolePermissions, tenantUow.RoleAssignments,
                loggerFactory.CreateLogger<OrganizationAdminBootstrapper>());
            organizationAdminGrantsCreated += await organizationAdminBootstrapper.ReconcilePermissionsAsync(
                tenant.Id.Value, nowUtc, cancellationToken);

            DefaultRoleCatalogueSeeder defaultRoleCatalogueSeeder = new(
                tenantUow.Roles, tenantUow.Permissions, tenantUow.RolePermissions,
                loggerFactory.CreateLogger<DefaultRoleCatalogueSeeder>());
            CondominiumRoleCatalogueSeeder condominiumRoleCatalogueSeeder = new(
                tenantUow.Roles, tenantUow.Permissions, tenantUow.RolePermissions,
                loggerFactory.CreateLogger<CondominiumRoleCatalogueSeeder>());
            ResidentRoleCatalogueSeeder residentRoleCatalogueSeeder = new(
                tenantUow.Roles, tenantUow.Permissions, tenantUow.RolePermissions,
                loggerFactory.CreateLogger<ResidentRoleCatalogueSeeder>());

            await defaultRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
            await condominiumRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
            await residentRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);

            await tenantUow.SaveChangesAsync(cancellationToken);
        }

        loggerFactory.CreateLogger<TenantRoleCatalogueBackfillSeeder>().LogInformation(
            "[DatabaseSeed] Tenant role/permission catalogue backfill: {TenantCount} tenant(s) reconciled, " +
            "{OrganizationAdminGrantsCreated} OrganizationAdmin grant(s) created",
            tenants.Count, organizationAdminGrantsCreated);
    }
}
