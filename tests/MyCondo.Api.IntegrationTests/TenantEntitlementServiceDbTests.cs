using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Real-PostgreSQL tests for <c>TenantEntitlementService</c> (ADR-033 §13, Task 05 §41) — exercises the
/// production resolver's repository orchestration against real persistence/effective-date semantics:
/// current-subscription retrieval, exact package-version composition, effective-override filtering
/// (including historical/expired exclusion), no-subscription behavior, and explicit-TenantId isolation.
/// These need a Docker daemon and were NOT executed in the environment they were authored in — see
/// PostgresApiFactory's doc comment. Run wherever Docker is available before trusting them.
/// </summary>
public class TenantEntitlementServiceDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public TenantEntitlementServiceDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static async Task<(FeatureDefinition Core, FeatureDefinition NonCore)> SeedCatalogueAsync(IServiceScope scope, string suffix)
    {
        IFeatureDefinitionRepository features = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        FeatureDefinition core = FeatureDefinition.Create(
            $"test.core.{suffix}", "Core Test Feature", null, null, "test", FeatureCatalogueStatus.Active, isCore: true, 5000, FeatureEntitlementType.Boolean);
        FeatureDefinition nonCore = FeatureDefinition.Create(
            $"test.optional.{suffix}", "Optional Test Feature", null, null, "test", FeatureCatalogueStatus.Active, isCore: false, 5001, FeatureEntitlementType.Boolean);

        features.Add(core);
        features.Add(nonCore);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return (core, nonCore);
    }

    private static async Task<SubscriptionPackageVersion> SeedPackageVersionAsync(
        IServiceScope scope, string suffix, IReadOnlyCollection<(FeatureDefinitionId FeatureId, bool Enabled)> assignments)
    {
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        ISubscriptionPackageFeatureRepository packageFeatures = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageFeatureRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{suffix}", "Test Package", null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, StartDate, null, 1000m, null, null, 10000m, "BDT");

        packages.Add(package);
        versions.Add(version);
        foreach ((FeatureDefinitionId featureId, bool enabled) in assignments)
        {
            packageFeatures.Add(new SubscriptionPackageFeature(version.Id, featureId, enabled, limitValue: null));
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        return version;
    }

    private static async Task<OrganizationSubscription> SeedSubscriptionAsync(IServiceScope scope, Guid tenantId, SubscriptionPackageVersionId versionId)
    {
        IOrganizationSubscriptionRepository subscriptions = scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenantId, versionId, BillingCycle.Monthly, StartDate, null, null, 1000m, 0m, "BDT", ActivatedAt, autoRenew: false);
        subscriptions.Add(subscription);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        return subscription;
    }

    [Fact]
    public async Task Core_Feature_Is_Enabled_With_No_Subscription_At_All()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        (FeatureDefinition core, _) = await SeedCatalogueAsync(scope, Guid.NewGuid().ToString("N")[..8]);
        Guid tenantId = Guid.NewGuid();

        using IServiceScope readScope = _factory.Services.CreateScope();
        bool enabled = await readScope.ServiceProvider.GetRequiredService<ITenantEntitlementService>()
            .IsFeatureEnabled(tenantId, core.Key, CancellationToken.None);

        enabled.Should().BeTrue();
    }

    [Fact]
    public async Task NonCore_Feature_Is_Disabled_With_No_Subscription_At_All()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        (_, FeatureDefinition nonCore) = await SeedCatalogueAsync(scope, Guid.NewGuid().ToString("N")[..8]);
        Guid tenantId = Guid.NewGuid();

        using IServiceScope readScope = _factory.Services.CreateScope();
        bool enabled = await readScope.ServiceProvider.GetRequiredService<ITenantEntitlementService>()
            .IsFeatureEnabled(tenantId, nonCore.Key, CancellationToken.None);

        enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Resolves_Against_The_Subscriptions_Exact_PackageVersion_Composition()
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        using IServiceScope scope = _factory.Services.CreateScope();
        (_, FeatureDefinition nonCore) = await SeedCatalogueAsync(scope, suffix);
        SubscriptionPackageVersion version = await SeedPackageVersionAsync(scope, suffix, [(nonCore.Id, true)]);
        Guid tenantId = Guid.NewGuid();
        await SeedSubscriptionAsync(scope, tenantId, version.Id);

        using IServiceScope readScope = _factory.Services.CreateScope();
        bool enabled = await readScope.ServiceProvider.GetRequiredService<ITenantEntitlementService>()
            .IsFeatureEnabled(tenantId, nonCore.Key, CancellationToken.None);

        enabled.Should().BeTrue();
    }

    [Fact]
    public async Task An_Effective_Override_Wins_Over_A_Disabled_Package_Row()
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        using IServiceScope scope = _factory.Services.CreateScope();
        (_, FeatureDefinition nonCore) = await SeedCatalogueAsync(scope, suffix);
        SubscriptionPackageVersion version = await SeedPackageVersionAsync(scope, suffix, [(nonCore.Id, false)]);
        Guid tenantId = Guid.NewGuid();
        await SeedSubscriptionAsync(scope, tenantId, version.Id);

        ITenantFeatureOverrideRepository overrides = scope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        overrides.Add(TenantFeatureOverride.Create(
            tenantId, nonCore, enabled: true, effectiveFrom: ActivatedAt, effectiveUntil: null,
            reason: "pilot", createdBy: Guid.NewGuid(), createdAtUtc: ActivatedAt));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        bool enabled = await readScope.ServiceProvider.GetRequiredService<ITenantEntitlementService>()
            .IsFeatureEnabled(tenantId, nonCore.Key, CancellationToken.None);

        enabled.Should().BeTrue();
    }

    [Fact]
    public async Task An_Expired_Override_Is_Excluded_And_The_Package_Row_Applies()
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        using IServiceScope scope = _factory.Services.CreateScope();
        (_, FeatureDefinition nonCore) = await SeedCatalogueAsync(scope, suffix);
        SubscriptionPackageVersion version = await SeedPackageVersionAsync(scope, suffix, [(nonCore.Id, false)]);
        Guid tenantId = Guid.NewGuid();
        await SeedSubscriptionAsync(scope, tenantId, version.Id);

        ITenantFeatureOverrideRepository overrides = scope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        overrides.Add(TenantFeatureOverride.Create(
            tenantId, nonCore, enabled: true, effectiveFrom: ActivatedAt.AddDays(-30), effectiveUntil: ActivatedAt.AddDays(-1),
            reason: "expired pilot", createdBy: Guid.NewGuid(), createdAtUtc: ActivatedAt.AddDays(-30)));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        bool enabled = await readScope.ServiceProvider.GetRequiredService<ITenantEntitlementService>()
            .IsFeatureEnabled(tenantId, nonCore.Key, CancellationToken.None);

        enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Entitlement_Resolution_Is_Isolated_By_Explicit_TenantId()
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        using IServiceScope scope = _factory.Services.CreateScope();
        (_, FeatureDefinition nonCore) = await SeedCatalogueAsync(scope, suffix);
        SubscriptionPackageVersion version = await SeedPackageVersionAsync(scope, suffix, [(nonCore.Id, true)]);
        Guid subscribedTenantId = Guid.NewGuid();
        Guid otherTenantId = Guid.NewGuid();
        await SeedSubscriptionAsync(scope, subscribedTenantId, version.Id);

        using IServiceScope readScope = _factory.Services.CreateScope();
        ITenantEntitlementService resolver = readScope.ServiceProvider.GetRequiredService<ITenantEntitlementService>();

        (await resolver.IsFeatureEnabled(subscribedTenantId, nonCore.Key, CancellationToken.None)).Should().BeTrue();
        (await resolver.IsFeatureEnabled(otherTenantId, nonCore.Key, CancellationToken.None)).Should().BeFalse();
    }
}
