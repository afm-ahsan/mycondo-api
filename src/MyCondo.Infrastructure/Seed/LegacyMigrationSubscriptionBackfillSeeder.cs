using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Authorization;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Infrastructure.Seed;

/// <summary>
/// ADR-033 §24 steps 3–4/7–8 (Task 04) — the compatibility/backfill bridge from legacy
/// <c>tenancy.tenant_modules</c> to the new <c>OrganizationSubscription</c> model. Runs in every
/// environment (not Development-only, same reasoning as <see cref="FinanceChartOfAccountBackfillSeeder"/>):
/// a real Staging/Production tenant provisioned before this migration exists has exactly the same gap a
/// dev tenant would.
///
/// <para><b>Core safety finding driving this seeder's design (Task 04 §12/§13):</b> <c>TenantModule</c>
/// was confirmed write-only — Task 00's full-repo grep found no authorization filter, Mediator pipeline
/// behavior, RLS policy, or tenant-facing UI that ever read it to gate access (ADR-033 §1). Its rows are
/// therefore not a reliable record of what a tenant could actually do; they are only a record of what a
/// Platform SuperAdmin happened to toggle in the console. Consequently the only backfill that provably
/// satisfies ADR-032/033's "no existing organization may lose a currently enabled capability" invariant
/// is to grant every migrated tenant the <em>full</em> Feature Catalogue (every
/// <see cref="FeatureCatalogueStatus.Active"/> feature, core and non-core alike) — not just the subset
/// implied by that tenant's own recorded module rows. This is deliberate, explicit over-entitlement
/// (Task 04 §3: "prefer temporary over-entitlement over accidental loss of service during the
/// compatibility period"), safe because no runtime enforcement exists yet (ADR-035, not this task) and
/// because every migrated tenant converges on the identical grant — so a single shared "grandfathered"
/// package/version, not one per distinct <c>TenantModule</c> row-set combination, is both sufficient and
/// simpler/more auditable (ADR-033 §24 step 3 leaves this an implementation-time call, bounded only by
/// "zero regression").</para>
///
/// <para>The legacy <c>TenantModule</c> row-set is still read and preserved — as
/// <see cref="PlatformAuditLogEntry.Metadata"/> on the per-tenant migration audit entry (informational/
/// shadow-comparison record) and as the input to <see cref="LegacyEntitlementShadowComparer"/>'s
/// verification pass — never as what gates the tenant's actual grandfathered entitlement.</para>
///
/// <para><b>Idempotency (Task 04 §14/§20):</b> no new column is added to distinguish "migrated" from
/// "not migrated." A tenant's mere possession of a current <see cref="OrganizationSubscription"/>
/// (<see cref="IOrganizationSubscriptionRepository.GetCurrentForTenantAsync"/>) is sufficient — the
/// existing partial-unique-index invariant already guarantees at most one such row, so this seeder never
/// overwrites a manually configured subscription, and a subsequent run is a pure no-op for any tenant
/// already covered (whether by this seeder or by a real commercial assignment made after go-live). A
/// migration-created subscription is structurally identifiable later, without a new column, by its
/// <see cref="OrganizationSubscription.PackageVersionId"/> pointing at the well-known
/// <see cref="GrandfatheredPackageCode"/> package's current version.</para>
///
/// <para><b>Failure isolation (Task 04 §19):</b> each tenant is processed in its own
/// <see cref="IServiceScope"/> (fresh <c>MyCondoDbContext</c>, no tracked-entity carryover from a
/// previous tenant's failure) with its own try/catch — one malformed tenant cannot abort or corrupt
/// another's backfill, and nothing is saved for a tenant until every check for that tenant has already
/// passed, so a failure never leaves partial <c>OrganizationSubscription</c>+audit state. A failed
/// tenant simply stays unmigrated and is retried the next time this seeder runs.</para>
///
/// <para><b>Legacy cohort boundary — reverted to "no current subscription" alone (ADR-033 Task 04B,
/// superseding Task 04A's timestamp gate):</b> Task 04A added a <c>Tenant.CreatedAtUtc</c> cutoff fixed at
/// the <c>OrganizationSubscription</c> schema migration's timestamp, reasoning that a tenant provisioned
/// after subscription-aware onboarding existed could lack a subscription for a non-legacy reason (partial
/// provisioning, a pending manual step, an onboarding regression) and would be wrongly grandfathered.
/// Task 04B verified that premise against the actual codebase and found it does not hold today: the
/// schema migration only made the <c>OrganizationSubscription</c> table possible to exist — it shipped no
/// change to tenant provisioning. <c>ProvisionOrganizationWithAdminCommandHandler</c> (the sole
/// tenant-provisioning entry point) still never creates an <c>OrganizationSubscription</c>, and a
/// repository-wide search confirms this seeder is the <em>only</em> code path in the entire application
/// that ever calls <see cref="OrganizationSubscription.Create"/> — there is no subscription-aware
/// onboarding flow yet to distinguish a tenant from (deferred to ADR-034/035, ADR-033 §"Provisioning
/// Reality"). Consequently a timestamp cutoff answers a question that has no current operational meaning:
/// every tenant that exists today, and every tenant this seeder will ever see until ADR-034/035 actually
/// ships, is provisioned through the identical non-subscription-aware flow — including ones created after
/// the Task 03 migration landed. Excluding those by creation time was actively wrong: it left genuine,
/// currently-provisioned production tenants ungrandfathered for no operational reason. "No current
/// <see cref="OrganizationSubscription"/>" is therefore sufficient eligibility on its own again, exactly
/// as Task 04 originally established, and remains safe for as long as the invariant above holds (zero
/// other callers of <c>OrganizationSubscription.Create</c>). <b>This must be revisited</b> the moment
/// ADR-034/035 introduces a second, subscription-aware provisioning path: if that path assigns a
/// subscription synchronously within the same provisioning transaction (as ADR-033's "extend the existing
/// provisioning command" direction describes), "no current subscription" continues to correctly identify
/// only pre-onboarding tenants with no additional gate needed; if it is instead asynchronous/eventual
/// (e.g. a pending-payment window), a new, non-timestamp cohort signal (e.g. an explicit provisioning-mode
/// marker on <see cref="Tenant"/>) must be introduced before that flow ships — not a revived timestamp.</para>
///
/// <para><b>Revisited (ADR-033 Task 12C):</b> <c>ProvisionOrganizationWithAdminCommandHandler</c> (Task
/// 12A) is exactly the synchronous-same-transaction case anticipated above — it creates the tenant's
/// <see cref="OrganizationSubscription"/> in the same <c>SaveChangesAsync</c> call as the tenant itself, so
/// "no current subscription" still correctly identifies only pre-Task-12A legacy tenants, no additional
/// gate needed. This eligibility rule is therefore unaffected by Task 12A/12B. It does <em>not</em>,
/// however, make every zero-subscription state momentary: this seeder's own per-tenant failure isolation
/// (retried next startup) and the <c>comparison.HasLoss</c> safety net below leave a tenant unmigrated —
/// and therefore still dependent on the lifecycle policy's no-subscription fallback — for an unbounded
/// window when either path is actually hit. Task 12C's lifecycle fallback decision accounts for this; see
/// <see cref="MyCondo.Application.Common.Abstractions.SubscriptionLifecyclePolicy"/>.</para>
/// </summary>
public sealed class LegacyMigrationSubscriptionBackfillSeeder(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory
)
{
    public const string GrandfatheredPackageCode = "LEGACY-MIGRATION-GRANDFATHERED";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        ILogger logger = loggerFactory.CreateLogger<LegacyMigrationSubscriptionBackfillSeeder>();

        List<FeatureDefinition> catalogue;
        List<Tenant> tenants;
        DateTimeOffset nowUtc;
        SubscriptionPackageVersion grandfatheredVersion;
        List<SubscriptionPackageFeature> grandfatheredFeatures;

        using (IServiceScope setupScope = scopeFactory.CreateScope())
        {
            IServiceProvider sp = setupScope.ServiceProvider;

            IClock clock = sp.GetRequiredService<IClock>();
            nowUtc = clock.UtcNow;

            catalogue = await sp.GetRequiredService<IFeatureDefinitionRepository>().GetAllAsync(cancellationToken);
            tenants = await sp.GetRequiredService<ITenantRepository>().GetAllAsync(cancellationToken);

            (grandfatheredVersion, grandfatheredFeatures) =
                await EnsureGrandfatheredPackageAsync(sp, catalogue, nowUtc, logger, cancellationToken);
        }

        int migrated = 0, skipped = 0, failed = 0;

        foreach (Tenant tenant in tenants)
        {
            using IServiceScope tenantScope = scopeFactory.CreateScope();
            IServiceProvider sp = tenantScope.ServiceProvider;

            try
            {
                IOrganizationSubscriptionRepository subscriptions =
                    sp.GetRequiredService<IOrganizationSubscriptionRepository>();

                OrganizationSubscription? current =
                    await subscriptions.GetCurrentForTenantAsync(tenant.Id.Value, cancellationToken);
                if (current is not null)
                {
                    skipped++;
                    continue;
                }

                List<TenantModule> enabledModules = await sp.GetRequiredService<ITenantModuleRepository>()
                    .GetEnabledForTenantAsync(tenant.Id.Value, cancellationToken);
                List<string> enabledModuleKeys = enabledModules.Select(m => m.ModuleKey).ToList();

                ShadowComparisonResult comparison = LegacyEntitlementShadowComparer.Compare(
                    tenant.Id.Value, enabledModuleKeys, catalogue, grandfatheredFeatures,
                    tenantOverrides: [], nowUtc);

                if (comparison.HasLoss)
                {
                    // Structurally unreachable given the grandfathered package enables the entire Active
                    // catalogue (see this class's own doc comment) — kept as a hard, loud safety net
                    // (Task 04 §34: "a loss must fail the test/backfill verification") rather than a
                    // silent accept, in case a future catalogue/package change ever invalidates that
                    // premise. This tenant is left unmigrated for manual review, never partially migrated.
                    logger.LogError(
                        "[DatabaseSeed] Legacy migration backfill: tenant {TenantId} shadow comparison " +
                        "found a LOSS ({LostFeatureKeys}) against the grandfathered package — refusing to " +
                        "migrate this tenant automatically; left unmigrated for manual review.",
                        tenant.Id.Value, string.Join(",", comparison.LostFeatureKeys));
                    failed++;
                    continue;
                }

                decimal basePrice = OrganizationSubscriptionCommercialTerms.ResolveBasePrice(
                    grandfatheredVersion, BillingCycle.Monthly);

                OrganizationSubscription subscription = OrganizationSubscription.Create(
                    tenant.Id.Value,
                    grandfatheredVersion.Id,
                    BillingCycle.Monthly,
                    startDate: DateOnly.FromDateTime(tenant.CreatedAtUtc.UtcDateTime),
                    endDate: null,
                    nextBillingDate: null,
                    basePrice: basePrice,
                    discount: 0m,
                    currency: grandfatheredVersion.Currency,
                    activatedAtUtc: nowUtc,
                    autoRenew: false);

                subscriptions.Add(subscription);

                List<string> reservedModulesObserved = enabledModuleKeys
                    .Where(key => LegacyTenantModuleFeatureMap.GetMapping(key).Kind == LegacyModuleMappingKind.ReservedCompatibility)
                    .ToList();
                bool reportingModuleObserved = enabledModuleKeys.Contains("reporting", StringComparer.Ordinal);

                sp.GetRequiredService<IPlatformAuditLogRepository>().Add(PlatformAuditLogEntry.Record(
                    nowUtc,
                    actorPlatformUserId: null,
                    action: "platform.subscription.assigned",
                    targetType: "OrganizationSubscription",
                    targetId: subscription.Id.Value.ToString(),
                    tenantId: tenant.Id.Value,
                    metadata: JsonSerializer.Serialize(new
                    {
                        source = "legacy-tenant-module-backfill",
                        packageCode = GrandfatheredPackageCode,
                        packageVersionId = grandfatheredVersion.Id.Value,
                        legacyEnabledModuleKeys = enabledModuleKeys,
                        reservedModulesObserved,
                        reportingModuleObserved,
                        gainedFeatureKeys = comparison.GainedFeatureKeys,
                    })));

                await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
                migrated++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[DatabaseSeed] Legacy migration backfill failed for tenant {TenantId} — left " +
                    "unmigrated; will retry on next startup.",
                    tenant.Id.Value);
                failed++;
            }
        }

        logger.LogInformation(
            "[DatabaseSeed] Legacy migration subscription backfill: {Migrated} tenant(s) migrated, " +
            "{Skipped} already had a subscription, {Failed} failed.",
            migrated, skipped, failed);
    }

    /// <summary>
    /// Idempotently creates the single shared grandfathered package/version/feature-composition, or
    /// loads it if a previous run already created it. Every <see cref="FeatureCatalogueStatus.Active"/>
    /// feature — including <c>IsCore</c> ones — gets an Enabled=true row: <see cref="FeatureDefinition.IsCore"/>
    /// features whose row is never actually consulted by the future resolver (ADR-033 §13 step 2
    /// short-circuits before reaching package lookup) are not filtered out here, because
    /// <c>property</c> (IsCore=true) has two IsCore=false children (<c>property.buildings</c>,
    /// <c>property.flats</c>) — <see cref="SubscriptionPackageFeatureComposition.Validate"/> requires
    /// every enabled leaf's full parent chain to also carry an enabled row in the <em>same</em> proposed
    /// set, so omitting the core parent's row would fail composition validation for its non-core
    /// children. Including every Active row (core included) is simpler and provably correct — the
    /// handful of redundant core rows cost nothing since they are never read for those features anyway.
    /// </summary>
    private static async Task<(SubscriptionPackageVersion Version, List<SubscriptionPackageFeature> Features)> EnsureGrandfatheredPackageAsync(
        IServiceProvider sp,
        List<FeatureDefinition> catalogue,
        DateTimeOffset nowUtc,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ISubscriptionPackageRepository packages = sp.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = sp.GetRequiredService<ISubscriptionPackageVersionRepository>();
        ISubscriptionPackageFeatureRepository packageFeatures = sp.GetRequiredService<ISubscriptionPackageFeatureRepository>();

        List<SubscriptionPackage> existingPackages = await packages.GetAllAsync(cancellationToken);
        SubscriptionPackage? existingPackage = existingPackages
            .SingleOrDefault(p => string.Equals(p.Code, GrandfatheredPackageCode, StringComparison.Ordinal));

        if (existingPackage is not null)
        {
            List<SubscriptionPackageVersion> existingVersions = await versions.GetAllAsync(cancellationToken);
            SubscriptionPackageVersion existingVersion = existingVersions.Single(v => v.Id == existingPackage.CurrentVersionId);

            List<SubscriptionPackageFeature> existingFeatures = (await packageFeatures.GetAllAsync(cancellationToken))
                .Where(f => f.PackageVersionId == existingVersion.Id)
                .ToList();

            return (existingVersion, existingFeatures);
        }

        SubscriptionPackage package = SubscriptionPackage.Create(
            GrandfatheredPackageCode,
            "Legacy Migration (Grandfathered)",
            "Not sellable — never list in a commercial pricing UI. ADR-033 §10/§24 migration-bridge " +
            "package, auto-assigned once to every pre-existing organization to preserve its actual " +
            "pre-migration access. See LegacyMigrationSubscriptionBackfillSeeder for the full rationale. " +
            "No commercial value assigned (BasePrice = 0).");

        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id,
            version: 1,
            effectiveFrom: DateOnly.FromDateTime(nowUtc.UtcDateTime),
            effectiveUntil: null,
            monthlyPrice: 0m,
            quarterlyPrice: null,
            semiAnnualPrice: null,
            annualPrice: null,
            currency: "BDT");

        List<SubscriptionPackageFeature> features = catalogue
            .Where(f => f.Status == FeatureCatalogueStatus.Active)
            .Select(f => new SubscriptionPackageFeature(version.Id, f.Id, enabled: true, limitValue: null))
            .ToList();

        // Fail loudly at seed time, not at some later read, if the mechanically generated composition
        // is ever invalid (e.g. a future Feature Catalogue change breaks a parent/child assumption).
        SubscriptionPackageFeatureComposition.Validate(version.Id, catalogue, features);

        package.Activate();
        version.Activate();
        package.SetCurrentVersion(version.Id);

        packages.Add(package);
        versions.Add(version);
        foreach (SubscriptionPackageFeature feature in features)
        {
            packageFeatures.Add(feature);
        }

        sp.GetRequiredService<IPlatformAuditLogRepository>().Add(PlatformAuditLogEntry.Record(
            nowUtc, actorPlatformUserId: null, action: "platform.package.created",
            targetType: "SubscriptionPackage", targetId: package.Id.Value.ToString()));
        sp.GetRequiredService<IPlatformAuditLogRepository>().Add(PlatformAuditLogEntry.Record(
            nowUtc, actorPlatformUserId: null, action: "platform.package.version.created",
            targetType: "SubscriptionPackageVersion", targetId: version.Id.Value.ToString(),
            metadata: JsonSerializer.Serialize(new { featureCount = features.Count })));

        await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "[DatabaseSeed] Legacy migration backfill: created grandfathered package '{Code}' version " +
            "{Version} with {FeatureCount} feature(s) enabled.",
            GrandfatheredPackageCode, version.Version, features.Count);

        return (version, features);
    }
}
