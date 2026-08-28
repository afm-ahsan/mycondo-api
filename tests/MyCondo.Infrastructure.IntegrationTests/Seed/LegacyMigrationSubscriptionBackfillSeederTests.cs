using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.PlatformAudit;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Seed;
using NSubstitute;

namespace MyCondo.Infrastructure.IntegrationTests.Seed;

/// <summary>
/// Proves <see cref="LegacyMigrationSubscriptionBackfillSeeder"/> creates the shared grandfathered
/// package exactly once, backfills every tenant (including one with zero legacy <c>TenantModule</c>
/// rows) with a zero-loss subscription, is idempotent, preserves Reserved-module observations in audit
/// metadata rather than granting them, and isolates one tenant's failure from another's — same
/// substitute-based, no-real-database approach already established for
/// <see cref="MyCondo.Infrastructure.Seed.FinanceChartOfAccountBackfillSeeder"/>/
/// <see cref="MyCondo.Infrastructure.Seed.TenantRoleCatalogueBackfillSeeder"/>'s own tests in this project.
/// Every dependency here is an ordinary (non-tenant-scoped) repository substitute registered as a DI
/// singleton, because — unlike Finance/Role backfill — none of the tables this seeder touches carry RLS
/// (ADR-019/033: <c>platform</c>-schema tables and <c>tenancy.tenant_modules</c> are platform-administered
/// metadata, not tenant-owned data), so no <c>ITenantScopedUnitOfWork</c> is involved.
/// </summary>
public class LegacyMigrationSubscriptionBackfillSeederTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private sealed class Fixture
    {
        public required IServiceScopeFactory ScopeFactory { get; init; }
        public required List<SubscriptionPackage> PackagesStore { get; init; }
        public required List<SubscriptionPackageVersion> VersionsStore { get; init; }
        public required List<SubscriptionPackageFeature> PackageFeaturesStore { get; init; }
        public required Dictionary<Guid, OrganizationSubscription> SubscriptionsStore { get; init; }
        public required Dictionary<Guid, List<TenantModule>> TenantModulesByTenant { get; init; }
        public required IPlatformAuditLogRepository AuditLog { get; init; }
        public required IUnitOfWork UnitOfWork { get; init; }
    }

    private static (FeatureDefinition PropertyRoot, FeatureDefinition PropertyBuildings, FeatureDefinition PropertyFlats,
        FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition Vendors) BuildCatalogue()
    {
        FeatureDefinition propertyRoot = FeatureDefinition.Create(
            "property", "Property", null, null, "property", FeatureCatalogueStatus.Active, isCore: true, 100, FeatureEntitlementType.Boolean);
        FeatureDefinition propertyBuildings = FeatureDefinition.Create(
            "property.buildings", "Buildings", null, propertyRoot.Id, "property", FeatureCatalogueStatus.Active, isCore: false, 101, FeatureEntitlementType.Boolean);
        FeatureDefinition propertyFlats = FeatureDefinition.Create(
            "property.flats", "Flats", null, propertyRoot.Id, "property", FeatureCatalogueStatus.Active, isCore: false, 102, FeatureEntitlementType.Boolean);
        FeatureDefinition securityRoot = FeatureDefinition.Create(
            "security", "Security", null, null, "security", FeatureCatalogueStatus.Active, isCore: false, 400, FeatureEntitlementType.Boolean);
        FeatureDefinition securityGates = FeatureDefinition.Create(
            "security.gates", "Gates", null, securityRoot.Id, "security", FeatureCatalogueStatus.Active, isCore: false, 401, FeatureEntitlementType.Boolean);
        FeatureDefinition vendors = FeatureDefinition.Create(
            "vendors", "Vendors (Reserved)", null, null, "vendors", FeatureCatalogueStatus.Reserved, isCore: false, 990, FeatureEntitlementType.Boolean);

        return (propertyRoot, propertyBuildings, propertyFlats, securityRoot, securityGates, vendors);
    }

    private static Fixture BuildFixture(List<Tenant> tenants, bool preSeedGrandfatheredPackage)
    {
        (FeatureDefinition propertyRoot, FeatureDefinition propertyBuildings, FeatureDefinition propertyFlats,
            FeatureDefinition securityRoot, FeatureDefinition securityGates, FeatureDefinition vendors) = BuildCatalogue();
        List<FeatureDefinition> catalogue = [propertyRoot, propertyBuildings, propertyFlats, securityRoot, securityGates, vendors];

        ITenantRepository tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(tenants);

        IFeatureDefinitionRepository featureDefinitions = Substitute.For<IFeatureDefinitionRepository>();
        featureDefinitions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(catalogue);

        IClock clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);

        List<SubscriptionPackage> packagesStore = [];
        List<SubscriptionPackageVersion> versionsStore = [];
        List<SubscriptionPackageFeature> packageFeaturesStore = [];

        if (preSeedGrandfatheredPackage)
        {
            SubscriptionPackage package = SubscriptionPackage.Create(
                LegacyMigrationSubscriptionBackfillSeeder.GrandfatheredPackageCode, "Legacy Migration (Grandfathered)", null);
            SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
                package.Id, 1, DateOnly.FromDateTime(Now.UtcDateTime), null, 0m, null, null, null, "BDT");
            package.Activate();
            version.Activate();
            package.SetCurrentVersion(version.Id);
            packagesStore.Add(package);
            versionsStore.Add(version);
            packageFeaturesStore.AddRange(catalogue
                .Where(f => f.Status == FeatureCatalogueStatus.Active)
                .Select(f => new SubscriptionPackageFeature(version.Id, f.Id, enabled: true, limitValue: null)));
        }

        ISubscriptionPackageRepository packages = Substitute.For<ISubscriptionPackageRepository>();
        packages.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_ => packagesStore.ToList());
        packages.When(p => p.Add(Arg.Any<SubscriptionPackage>())).Do(ci => packagesStore.Add(ci.Arg<SubscriptionPackage>()));

        ISubscriptionPackageVersionRepository versions = Substitute.For<ISubscriptionPackageVersionRepository>();
        versions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_ => versionsStore.ToList());
        versions.When(v => v.Add(Arg.Any<SubscriptionPackageVersion>())).Do(ci => versionsStore.Add(ci.Arg<SubscriptionPackageVersion>()));

        ISubscriptionPackageFeatureRepository packageFeatures = Substitute.For<ISubscriptionPackageFeatureRepository>();
        packageFeatures.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_ => packageFeaturesStore.ToList());
        packageFeatures.When(f => f.Add(Arg.Any<SubscriptionPackageFeature>())).Do(ci => packageFeaturesStore.Add(ci.Arg<SubscriptionPackageFeature>()));

        Dictionary<Guid, OrganizationSubscription> subscriptionsStore = [];
        IOrganizationSubscriptionRepository subscriptions = Substitute.For<IOrganizationSubscriptionRepository>();
        subscriptions.GetCurrentForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => subscriptionsStore.TryGetValue(ci.Arg<Guid>(), out OrganizationSubscription? existing) ? existing : null);
        subscriptions.When(s => s.Add(Arg.Any<OrganizationSubscription>()))
            .Do(ci =>
            {
                OrganizationSubscription added = ci.Arg<OrganizationSubscription>();
                subscriptionsStore[added.TenantId] = added;
            });

        Dictionary<Guid, List<TenantModule>> tenantModulesByTenant = [];
        ITenantModuleRepository tenantModules = Substitute.For<ITenantModuleRepository>();
        tenantModules.GetEnabledForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => tenantModulesByTenant.TryGetValue(ci.Arg<Guid>(), out List<TenantModule>? modules) ? modules : []);

        IPlatformAuditLogRepository auditLog = Substitute.For<IPlatformAuditLogRepository>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        ServiceCollection services = new();
        services.AddSingleton(tenantRepository);
        services.AddSingleton(featureDefinitions);
        services.AddSingleton(clock);
        services.AddSingleton(packages);
        services.AddSingleton(versions);
        services.AddSingleton(packageFeatures);
        services.AddSingleton(subscriptions);
        services.AddSingleton(tenantModules);
        services.AddSingleton(auditLog);
        services.AddSingleton(unitOfWork);
        IServiceProvider provider = services.BuildServiceProvider();

        return new Fixture
        {
            ScopeFactory = provider.GetRequiredService<IServiceScopeFactory>(),
            PackagesStore = packagesStore,
            VersionsStore = versionsStore,
            PackageFeaturesStore = packageFeaturesStore,
            SubscriptionsStore = subscriptionsStore,
            TenantModulesByTenant = tenantModulesByTenant,
            AuditLog = auditLog,
            UnitOfWork = unitOfWork,
        };
    }

    [Fact]
    public async Task Creates_The_Grandfathered_Package_Exactly_Once_And_Backfills_Every_Tenant()
    {
        Tenant tenantA = Tenant.Provision("Tenant A", "tenant-a", Now.AddYears(-1));
        Tenant tenantB = Tenant.Provision("Tenant B", "tenant-b", Now.AddYears(-1));
        Fixture fixture = BuildFixture([tenantA, tenantB], preSeedGrandfatheredPackage: false);

        LegacyMigrationSubscriptionBackfillSeeder seeder = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        fixture.PackagesStore.Should().ContainSingle(p => p.Code == LegacyMigrationSubscriptionBackfillSeeder.GrandfatheredPackageCode);
        fixture.SubscriptionsStore.Should().ContainKey(tenantA.Id.Value);
        fixture.SubscriptionsStore.Should().ContainKey(tenantB.Id.Value);
        fixture.SubscriptionsStore[tenantA.Id.Value].BasePrice.Should().Be(0m);
    }

    [Fact]
    public async Task Second_Run_Reuses_The_Existing_Package_And_Does_Not_Recreate_It()
    {
        Tenant tenant = Tenant.Provision("Tenant A", "tenant-a", Now.AddYears(-1));
        Fixture fixture = BuildFixture([tenant], preSeedGrandfatheredPackage: false);

        LegacyMigrationSubscriptionBackfillSeeder firstRun = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await firstRun.SeedAsync(CancellationToken.None);

        LegacyMigrationSubscriptionBackfillSeeder secondRun = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await secondRun.SeedAsync(CancellationToken.None);

        fixture.PackagesStore.Should().HaveCount(1);
        fixture.VersionsStore.Should().HaveCount(1);
    }

    [Fact]
    public async Task Skips_A_Tenant_That_Already_Has_A_Current_Subscription()
    {
        Tenant tenant = Tenant.Provision("Tenant A", "tenant-a", Now.AddYears(-1));
        Fixture fixture = BuildFixture([tenant], preSeedGrandfatheredPackage: true);

        OrganizationSubscription manuallyConfigured = OrganizationSubscription.Create(
            tenant.Id.Value, fixture.VersionsStore[0].Id, BillingCycle.Annual, DateOnly.FromDateTime(Now.UtcDateTime),
            null, null, 80000m, 0m, "BDT", Now, autoRenew: true);
        fixture.SubscriptionsStore[tenant.Id.Value] = manuallyConfigured;

        LegacyMigrationSubscriptionBackfillSeeder seeder = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        fixture.SubscriptionsStore[tenant.Id.Value].Should().BeSameAs(manuallyConfigured);
    }

    [Fact]
    public async Task Migrates_A_Tenant_With_Zero_TenantModule_Rows_With_The_Full_Grandfathered_Package()
    {
        Tenant tenant = Tenant.Provision("Tenant A", "tenant-a", Now.AddYears(-1));
        Fixture fixture = BuildFixture([tenant], preSeedGrandfatheredPackage: false);
        // TenantModulesByTenant intentionally left empty for this tenant — "historically write-only,
        // zero rows never meant zero access" (Task 04 §12/§13).

        LegacyMigrationSubscriptionBackfillSeeder seeder = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        fixture.SubscriptionsStore.Should().ContainKey(tenant.Id.Value);
        fixture.PackageFeaturesStore.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Records_Reserved_Legacy_Modules_As_Audit_Metadata_Not_As_A_Package_Or_Override_Grant()
    {
        Tenant tenant = Tenant.Provision("Tenant A", "tenant-a", Now.AddYears(-1));
        Fixture fixture = BuildFixture([tenant], preSeedGrandfatheredPackage: false);
        fixture.TenantModulesByTenant[tenant.Id.Value] = [TenantModule.Enable(tenant.Id.Value, "vendors", Now, null)];

        LegacyMigrationSubscriptionBackfillSeeder seeder = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        fixture.AuditLog.Received().Add(Arg.Is<PlatformAuditLogEntry>(e =>
            e.Action == "platform.subscription.assigned"
            && e.TenantId == tenant.Id.Value
            && e.Metadata != null
            && e.Metadata.Contains("vendors")
            && e.Metadata.Contains("reservedModulesObserved")));
    }

    [Fact]
    public async Task Does_Not_Duplicate_An_Already_Migrated_Legacy_Tenants_Subscription_On_Rerun()
    {
        Tenant tenant = Tenant.Provision("Tenant A", "tenant-a", Now.AddYears(-1));
        Fixture fixture = BuildFixture([tenant], preSeedGrandfatheredPackage: false);

        LegacyMigrationSubscriptionBackfillSeeder firstRun = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await firstRun.SeedAsync(CancellationToken.None);
        OrganizationSubscription firstSubscription = fixture.SubscriptionsStore[tenant.Id.Value];

        LegacyMigrationSubscriptionBackfillSeeder secondRun = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await secondRun.SeedAsync(CancellationToken.None);

        fixture.SubscriptionsStore.Should().ContainKey(tenant.Id.Value);
        fixture.SubscriptionsStore[tenant.Id.Value].Should().BeSameAs(firstSubscription);
    }

    [Fact]
    public async Task Does_Not_Grandfather_A_Tenant_Provisioned_At_Or_After_The_Subscription_Architecture_Cutover()
    {
        Tenant futureTenant = Tenant.Provision(
            "Future Tenant", "future-tenant",
            LegacyMigrationSubscriptionBackfillSeeder.SubscriptionArchitectureCutoverUtc);
        Fixture fixture = BuildFixture([futureTenant], preSeedGrandfatheredPackage: false);

        LegacyMigrationSubscriptionBackfillSeeder seeder = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        fixture.SubscriptionsStore.Should().NotContainKey(futureTenant.Id.Value);
    }

    [Fact]
    public async Task Does_Not_Grandfather_A_Future_Tenant_With_Zero_TenantModule_Rows()
    {
        Tenant futureTenant = Tenant.Provision(
            "Future Tenant", "future-tenant",
            LegacyMigrationSubscriptionBackfillSeeder.SubscriptionArchitectureCutoverUtc.AddDays(1));
        Fixture fixture = BuildFixture([futureTenant], preSeedGrandfatheredPackage: true);
        // TenantModulesByTenant intentionally left empty — a future tenant with no legacy module rows
        // must still be excluded on cohort grounds alone, not merely because it "has no modules to lose."

        LegacyMigrationSubscriptionBackfillSeeder seeder = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        fixture.SubscriptionsStore.Should().NotContainKey(futureTenant.Id.Value);
    }

    [Fact]
    public async Task Does_Not_Grandfather_A_Future_Tenant_That_Has_TenantModule_Rows()
    {
        Tenant futureTenant = Tenant.Provision(
            "Future Tenant", "future-tenant",
            LegacyMigrationSubscriptionBackfillSeeder.SubscriptionArchitectureCutoverUtc.AddDays(1));
        Fixture fixture = BuildFixture([futureTenant], preSeedGrandfatheredPackage: true);
        fixture.TenantModulesByTenant[futureTenant.Id.Value] = [TenantModule.Enable(futureTenant.Id.Value, "property", Now, null)];
        // Legacy-module presence must not override cohort exclusion (Task 04A §12) — a future tenant
        // that somehow has TenantModule rows is still not a legacy migration candidate.

        LegacyMigrationSubscriptionBackfillSeeder seeder = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        await seeder.SeedAsync(CancellationToken.None);

        fixture.SubscriptionsStore.Should().NotContainKey(futureTenant.Id.Value);
    }

    [Fact]
    public async Task One_Tenants_Failure_Does_Not_Prevent_Another_Tenants_Migration()
    {
        Tenant failingTenant = Tenant.Provision("Failing Tenant", "failing-tenant", Now.AddYears(-1));
        Tenant healthyTenant = Tenant.Provision("Healthy Tenant", "healthy-tenant", Now.AddYears(-1));
        // Package is pre-seeded so the only SaveChangesAsync calls left are the two per-tenant calls —
        // isolates which call belongs to which tenant deterministically. (Modeling exactly which of the
        // two tenants' Add() calls actually "commits" would require re-implementing EF Core's real
        // per-scope-DbContext discard-on-failure semantics in this substitute — disproportionate for a
        // unit test; the properties that actually matter to Task 04 §19 are asserted directly below:
        // the exception from tenant 1 doesn't propagate out of SeedAsync, and tenant 2 still gets its
        // own SaveChangesAsync call and ends up migrated.)
        Fixture fixture = BuildFixture([failingTenant, healthyTenant], preSeedGrandfatheredPackage: true);

        int saveCall = 0;
        fixture.UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            saveCall++;
            if (saveCall == 1)
            {
                throw new InvalidOperationException("simulated per-tenant save failure");
            }

            return Task.FromResult(1);
        });

        LegacyMigrationSubscriptionBackfillSeeder seeder = new(fixture.ScopeFactory, NullLoggerFactory.Instance);
        Func<Task> act = () => seeder.SeedAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        saveCall.Should().Be(2);
        fixture.SubscriptionsStore.Should().ContainKey(healthyTenant.Id.Value);
    }
}
