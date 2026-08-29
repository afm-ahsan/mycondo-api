using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;

/// <summary>
/// Provisions a new organization AND its founding administrator in one atomic operation — the
/// counterpart to the two-step dance the pre-existing Platform metadata-only provisioning +
/// separate tenant self-registration bootstrap otherwise required. Reuses the exact same
/// bootstrap/role-catalogue services <see cref="Auth.Commands.Register.RegisterUserCommandHandler"/>
/// already composes for "first user of a tenant" — no parallel bootstrap logic is introduced.
///
/// Writes to RLS-protected <c>identity.*</c> tables (the admin user, their role grant, the seeded
/// role catalogues) via <see cref="ITenantScopedUnitOfWork"/> rather than the ambient request-scoped
/// unit of work, because a Platform-authenticated request has no tenant JWT claim to establish RLS's
/// tenant context from (see ADR-019) — the tenant-scoped unit of work declares the brand-new tenant's
/// own id as that context, which is legitimate here because this operation is the one creating that
/// tenant. <see cref="ITenantRepository"/> reads (slug/code uniqueness) still use the ambient,
/// DI-injected repository since <c>tenancy.tenants</c> carries no RLS policy at all.
/// </summary>
public sealed class ProvisionOrganizationWithAdminCommandHandler(
    ITenantRepository tenants,
    ISubscriptionPackageRepository subscriptionPackages,
    ISubscriptionPackageVersionRepository subscriptionPackageVersions,
    ITenantScopedUnitOfWorkFactory tenantScopedUnitOfWorkFactory,
    IPasswordHasher passwordHasher,
    IClock clock,
    ICurrentPlatformUserProvider currentPlatformUser,
    ILoggerFactory loggerFactory,
    ILogger<ProvisionOrganizationWithAdminCommandHandler> logger
) : IRequestHandler<ProvisionOrganizationWithAdminCommand, ProvisionOrganizationResult>
{
    public async ValueTask<ProvisionOrganizationResult> Handle(
        ProvisionOrganizationWithAdminCommand command, CancellationToken cancellationToken)
    {
        string normalizedSlug = command.Slug.Trim().ToLowerInvariant();
        string normalizedCode = command.Code.Trim().ToUpperInvariant();
        string normalizedEmail = command.AdministratorEmail.Trim().ToLowerInvariant();

        if (await tenants.SlugExistsAsync(normalizedSlug, cancellationToken))
        {
            throw new ConflictException($"An organization with slug '{normalizedSlug}' already exists.");
        }

        if (await tenants.CodeExistsAsync(normalizedCode, cancellationToken))
        {
            throw new ConflictException($"An organization with code '{normalizedCode}' already exists.");
        }

        SubscriptionPackageVersion packageVersion = await ResolveAssignablePackageVersionAsync(
            command.SubscriptionPackageVersionId, cancellationToken);

        DateTimeOffset nowUtc = clock.UtcNow;
        TenantId tenantId = TenantId.New();

        await using ITenantScopedUnitOfWork uow = tenantScopedUnitOfWorkFactory.Create(tenantId.Value);

        Tenant tenant = Tenant.Provision(tenantId, command.Name, normalizedSlug, nowUtc);
        tenant.UpdateDetails(command.Name, normalizedCode, nowUtc);
        tenant.Activate(nowUtc);
        uow.Tenants.Add(tenant);

        string passwordHash = passwordHasher.Hash(command.AdministratorPassword);
        User admin = User.Register(
            tenant.Id.Value, normalizedEmail, passwordHash, command.AdministratorFullName, phoneNumber: null, nowUtc);
        uow.Users.Add(admin);

        tenant.SetPrimaryAdministrator(admin.Id.Value, admin.FullName, admin.Email);

        // Monthly is the only billing cycle this provisioning path offers — negotiated cycle/price terms
        // are out of Task 12A's scope (ADR-033 §11: list price is only ever a starting snapshot). A
        // package version with no MonthlyPrice fails ResolveBasePrice with the existing
        // UnsupportedBillingCycleException, which is the correct rejection for that case.
        decimal basePrice = OrganizationSubscriptionCommercialTerms.ResolveBasePrice(packageVersion, BillingCycle.Monthly);
        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenant.Id.Value,
            packageVersion.Id,
            BillingCycle.Monthly,
            startDate: DateOnly.FromDateTime(nowUtc.UtcDateTime),
            endDate: null,
            nextBillingDate: null,
            basePrice: basePrice,
            discount: 0m,
            currency: packageVersion.Currency,
            activatedAtUtc: nowUtc,
            autoRenew: true);
        uow.OrganizationSubscriptions.Add(subscription);

        OrganizationAdminBootstrapper organizationAdminBootstrapper = new(
            uow.Roles, uow.Permissions, uow.RolePermissions, uow.RoleAssignments,
            loggerFactory.CreateLogger<OrganizationAdminBootstrapper>());
        await organizationAdminBootstrapper.BootstrapAsync(tenant.Id.Value, admin, nowUtc, cancellationToken);

        DefaultRoleCatalogueSeeder defaultRoleCatalogueSeeder = new(
            uow.Roles, uow.Permissions, uow.RolePermissions, loggerFactory.CreateLogger<DefaultRoleCatalogueSeeder>());
        CondominiumRoleCatalogueSeeder condominiumRoleCatalogueSeeder = new(
            uow.Roles, uow.Permissions, uow.RolePermissions, loggerFactory.CreateLogger<CondominiumRoleCatalogueSeeder>());
        ResidentRoleCatalogueSeeder residentRoleCatalogueSeeder = new(
            uow.Roles, uow.Permissions, uow.RolePermissions, loggerFactory.CreateLogger<ResidentRoleCatalogueSeeder>());
        ExpenseCategoryCatalogueSeeder expenseCategoryCatalogueSeeder = new(
            uow.ExpenseCategories, uow.ExpenseTypes, loggerFactory.CreateLogger<ExpenseCategoryCatalogueSeeder>());
        ExpenseTypeCatalogueSeeder expenseTypeCatalogueSeeder = new(
            uow.ExpenseTypes, uow.ExpenseCategories, loggerFactory.CreateLogger<ExpenseTypeCatalogueSeeder>());

        await defaultRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
        await condominiumRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
        await residentRoleCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);

        // Category must be saved before the type seeder resolves categories — see
        // RegisterUserCommandHandler's identical ordering comment.
        await expenseCategoryCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        await expenseTypeCatalogueSeeder.SeedAsync(tenant.Id.Value, nowUtc, cancellationToken);

        await uow.TenantModules.ReplaceForTenantAsync(
            tenant.Id.Value, command.EnabledModuleKeys, nowUtc, currentPlatformUser.PlatformUserId, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organization {TenantId} provisioned with administrator {AdminUserId} by platform user {PlatformUserId}",
            tenant.Id, admin.Id, currentPlatformUser.PlatformUserId);

        return new ProvisionOrganizationResult(
            tenant.Id.Value, tenant.Name, tenant.Code!, tenant.Slug, tenant.Status.ToString(), admin.Id.Value);
    }

    /// <summary>
    /// Validates the caller-supplied package version the way ADR-033's package/version model already
    /// requires a "currently offered" version to look: <see cref="SubscriptionPackageVersion.Status"/>
    /// Active alone is the deterministic "current version" signal, but this also cross-checks
    /// <see cref="SubscriptionPackage.CurrentVersionId"/> and the package's own Active status as
    /// defense-in-depth against the two ever drifting apart (they are set by two separate calls — see
    /// <c>LegacyMigrationSubscriptionBackfillSeeder.EnsureGrandfatheredPackageAsync</c>, the only other
    /// caller of this pair today). No new repository query methods are added — both repositories only
    /// expose <c>GetAllAsync</c> today (see <see cref="ISubscriptionPackageVersionRepository"/>/
    /// <see cref="ISubscriptionPackageRepository"/>), and the seeder above already establishes that
    /// in-memory-filter pattern as this codebase's existing way of doing this lookup.
    /// </summary>
    private async Task<SubscriptionPackageVersion> ResolveAssignablePackageVersionAsync(
        Guid subscriptionPackageVersionId, CancellationToken cancellationToken)
    {
        SubscriptionPackageVersionId versionId = new(subscriptionPackageVersionId);

        SubscriptionPackageVersion? version = (await subscriptionPackageVersions.GetAllAsync(cancellationToken))
            .SingleOrDefault(v => v.Id == versionId);
        if (version is null)
        {
            throw new NotFoundException("SubscriptionPackageVersion", subscriptionPackageVersionId);
        }

        if (version.Status != SubscriptionPackageVersionStatus.Active)
        {
            throw new ConflictException(
                $"Subscription package version '{subscriptionPackageVersionId}' is not commercially active and cannot be assigned.");
        }

        SubscriptionPackage? package = (await subscriptionPackages.GetAllAsync(cancellationToken))
            .SingleOrDefault(p => p.Id == version.PackageId);
        if (package is null || package.Status != SubscriptionPackageStatus.Active || package.CurrentVersionId != version.Id)
        {
            throw new ConflictException(
                $"Subscription package version '{subscriptionPackageVersionId}' does not belong to an active, currently offered subscription package.");
        }

        // The "LEGACY-MIGRATION-GRANDFATHERED" package (LegacyMigrationSubscriptionBackfillSeeder) is a
        // migration-bridge artifact, never a commercial offering — Task 12A must not let it be assigned
        // to a newly provisioned tenant even explicitly. Literal duplicated here, not referenced from
        // Infrastructure, to keep Application's dependency direction intact (Application must not
        // reference Infrastructure).
        if (string.Equals(package.Code, "LEGACY-MIGRATION-GRANDFATHERED", StringComparison.Ordinal))
        {
            throw new ConflictException(
                "The legacy migration grandfathered package cannot be assigned to a newly provisioned organization.");
        }

        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        if (version.EffectiveFrom > today || (version.EffectiveUntil is not null && version.EffectiveUntil < today))
        {
            throw new ConflictException(
                $"Subscription package version '{subscriptionPackageVersionId}' is not effective as of {today:yyyy-MM-dd}.");
        }

        return version;
    }
}
