using AwesomeAssertions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.FeatureCatalogue.Exceptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

/// <summary>
/// Application-layer tests for <see cref="TenantEntitlementService"/> (ADR-033 §13, Task 05 §40) —
/// verifies repository orchestration (exact PackageVersionId used, effective-override date filtering via
/// <see cref="IClock"/>, no-subscription short-circuit) and query-count sanity (§43), on top of the
/// precedence cases already covered at the pure-function level by
/// <c>TenantEntitlementResolutionTests</c>.
/// </summary>
public class TenantEntitlementServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IFeatureDefinitionRepository _featureDefinitions = Substitute.For<IFeatureDefinitionRepository>();
    private readonly IOrganizationSubscriptionRepository _subscriptions = Substitute.For<IOrganizationSubscriptionRepository>();
    private readonly ISubscriptionPackageFeatureRepository _packageFeatures = Substitute.For<ISubscriptionPackageFeatureRepository>();
    private readonly ITenantFeatureOverrideRepository _overrides = Substitute.For<ITenantFeatureOverrideRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly FeatureDefinition CoreFeature = FeatureDefinition.Create(
        "property", "Property", null, null, "property", FeatureCatalogueStatus.Active, isCore: true, 1, FeatureEntitlementType.Boolean);

    private static readonly FeatureDefinition NonCoreFeature = FeatureDefinition.Create(
        "finance.expenses", "Expenses", null, null, "finance", FeatureCatalogueStatus.Active, isCore: false, 2, FeatureEntitlementType.Boolean);

    public TenantEntitlementServiceTests()
    {
        _clock.UtcNow.Returns(Now);
        _featureDefinitions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([CoreFeature, NonCoreFeature]);
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns((OrganizationSubscription?)null);
        _packageFeatures.GetForPackageVersionAsync(Arg.Any<SubscriptionPackageVersionId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _overrides.GetForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([]);
    }

    private TenantEntitlementService CreateService() =>
        new(_featureDefinitions, _subscriptions, _packageFeatures, _overrides, _clock);

    private static OrganizationSubscription CreateSubscription(SubscriptionPackageVersionId versionId) =>
        OrganizationSubscription.Create(
            TenantId, versionId, BillingCycle.Monthly, startDate: DateOnly.FromDateTime(Now.UtcDateTime),
            endDate: null, nextBillingDate: null, basePrice: 100m, discount: 0m, currency: "BDT",
            activatedAtUtc: Now, autoRenew: false);

    [Fact]
    public async Task IsFeatureEnabled_Throws_For_An_Unknown_Feature_Key()
    {
        TenantEntitlementService sut = CreateService();

        Func<Task> act = () => sut.IsFeatureEnabled(TenantId, "no.such.feature", CancellationToken.None);

        await act.Should().ThrowAsync<FeatureNotFoundException>();
    }

    [Fact]
    public async Task Core_Feature_Is_Enabled_With_No_Subscription()
    {
        TenantEntitlementService sut = CreateService();

        bool enabled = await sut.IsFeatureEnabled(TenantId, "property", CancellationToken.None);

        enabled.Should().BeTrue();
    }

    [Fact]
    public async Task NonCore_Feature_Is_Disabled_With_No_Subscription_Even_With_A_Matching_Override()
    {
        TenantFeatureOverride enablingOverride = TenantFeatureOverride.Create(
            TenantId, NonCoreFeature, enabled: true, effectiveFrom: Now.AddDays(-1), effectiveUntil: null,
            reason: "test", createdBy: Guid.NewGuid(), createdAtUtc: Now.AddDays(-1));
        _overrides.GetForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([enablingOverride]);
        TenantEntitlementService sut = CreateService();

        bool enabled = await sut.IsFeatureEnabled(TenantId, "finance.expenses", CancellationToken.None);

        enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Resolver_Loads_Package_Features_For_The_Subscriptions_Exact_PackageVersionId()
    {
        SubscriptionPackageVersionId versionId = SubscriptionPackageVersionId.New();
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(CreateSubscription(versionId));
        _packageFeatures.GetForPackageVersionAsync(versionId, Arg.Any<CancellationToken>())
            .Returns([new SubscriptionPackageFeature(versionId, NonCoreFeature.Id, enabled: true, limitValue: null)]);
        TenantEntitlementService sut = CreateService();

        bool enabled = await sut.IsFeatureEnabled(TenantId, "finance.expenses", CancellationToken.None);

        enabled.Should().BeTrue();
        await _packageFeatures.Received(1).GetForPackageVersionAsync(versionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_Override_Outside_Its_Effective_Window_Is_Ignored()
    {
        SubscriptionPackageVersionId versionId = SubscriptionPackageVersionId.New();
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(CreateSubscription(versionId));
        TenantFeatureOverride expiredOverride = TenantFeatureOverride.Create(
            TenantId, NonCoreFeature, enabled: true, effectiveFrom: Now.AddDays(-10), effectiveUntil: Now.AddDays(-1),
            reason: "expired pilot", createdBy: Guid.NewGuid(), createdAtUtc: Now.AddDays(-10));
        _overrides.GetForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([expiredOverride]);
        TenantEntitlementService sut = CreateService();

        bool enabled = await sut.IsFeatureEnabled(TenantId, "finance.expenses", CancellationToken.None);

        enabled.Should().BeFalse();
    }

    [Fact]
    public async Task An_Effective_Override_Is_Honored()
    {
        SubscriptionPackageVersionId versionId = SubscriptionPackageVersionId.New();
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(CreateSubscription(versionId));
        TenantFeatureOverride effectiveOverride = TenantFeatureOverride.Create(
            TenantId, NonCoreFeature, enabled: true, effectiveFrom: Now.AddDays(-1), effectiveUntil: Now.AddDays(1),
            reason: "pilot", createdBy: Guid.NewGuid(), createdAtUtc: Now.AddDays(-1));
        _overrides.GetForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns([effectiveOverride]);
        TenantEntitlementService sut = CreateService();

        bool enabled = await sut.IsFeatureEnabled(TenantId, "finance.expenses", CancellationToken.None);

        enabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetEntitlementValue_Returns_Null_For_A_Boolean_Feature()
    {
        TenantEntitlementService sut = CreateService();

        int? value = await sut.GetEntitlementValue(TenantId, "property", CancellationToken.None);

        value.Should().BeNull();
    }

    [Fact]
    public async Task IsFeatureEnabled_Agrees_With_GetEffectiveEntitlements_For_Every_Feature()
    {
        SubscriptionPackageVersionId versionId = SubscriptionPackageVersionId.New();
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(CreateSubscription(versionId));
        _packageFeatures.GetForPackageVersionAsync(versionId, Arg.Any<CancellationToken>())
            .Returns([new SubscriptionPackageFeature(versionId, NonCoreFeature.Id, enabled: true, limitValue: null)]);
        TenantEntitlementService sut = CreateService();

        IReadOnlyDictionary<string, bool> all = await sut.GetEffectiveEntitlements(TenantId, CancellationToken.None);

        foreach ((string key, bool expected) in all)
        {
            bool actual = await sut.IsFeatureEnabled(TenantId, key, CancellationToken.None);
            actual.Should().Be(expected, $"IsFeatureEnabled and GetEffectiveEntitlements must agree for '{key}'");
        }
    }

    [Fact]
    public async Task GetEffectiveEntitlements_Issues_Exactly_Four_Queries_Regardless_Of_Catalogue_Size()
    {
        SubscriptionPackageVersionId versionId = SubscriptionPackageVersionId.New();
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(CreateSubscription(versionId));
        TenantEntitlementService sut = CreateService();

        await sut.GetEffectiveEntitlements(TenantId, CancellationToken.None);

        await _featureDefinitions.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await _subscriptions.Received(1).GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>());
        await _packageFeatures.Received(1).GetForPackageVersionAsync(versionId, Arg.Any<CancellationToken>());
        await _overrides.Received(1).GetForTenantAsync(TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEffectiveEntitlementDetails_Agrees_With_GetEffectiveEntitlements_For_Every_Feature()
    {
        SubscriptionPackageVersionId versionId = SubscriptionPackageVersionId.New();
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(CreateSubscription(versionId));
        _packageFeatures.GetForPackageVersionAsync(versionId, Arg.Any<CancellationToken>())
            .Returns([new SubscriptionPackageFeature(versionId, NonCoreFeature.Id, enabled: true, limitValue: null)]);
        TenantEntitlementService sut = CreateService();

        IReadOnlyDictionary<string, bool> flat = await sut.GetEffectiveEntitlements(TenantId, CancellationToken.None);
        IReadOnlyList<EffectiveEntitlement> details = await sut.GetEffectiveEntitlementDetails(TenantId, CancellationToken.None);

        details.Should().HaveCount(flat.Count);
        foreach (EffectiveEntitlement entitlement in details)
        {
            flat.Should().ContainKey(entitlement.FeatureKey);
            entitlement.Enabled.Should().Be(flat[entitlement.FeatureKey]);
        }
    }

    [Fact]
    public async Task GetEffectiveEntitlementDetails_Carries_LimitValue_For_A_Numeric_Package_Feature()
    {
        FeatureDefinition numericFeature = FeatureDefinition.Create(
            "utilities.meters", "Meters", null, null, "utilities", FeatureCatalogueStatus.Active,
            isCore: false, 3, FeatureEntitlementType.Numeric);
        _featureDefinitions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([CoreFeature, NonCoreFeature, numericFeature]);
        SubscriptionPackageVersionId versionId = SubscriptionPackageVersionId.New();
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(CreateSubscription(versionId));
        _packageFeatures.GetForPackageVersionAsync(versionId, Arg.Any<CancellationToken>())
            .Returns([new SubscriptionPackageFeature(versionId, numericFeature.Id, enabled: true, limitValue: 5)]);
        TenantEntitlementService sut = CreateService();

        IReadOnlyList<EffectiveEntitlement> details = await sut.GetEffectiveEntitlementDetails(TenantId, CancellationToken.None);

        EffectiveEntitlement meters = details.Single(e => e.FeatureKey == "utilities.meters");
        meters.Enabled.Should().BeTrue();
        meters.LimitValue.Should().Be(5);
    }

    [Fact]
    public async Task GetEffectiveEntitlementDetails_Never_Marks_A_Reserved_Feature_Enabled()
    {
        FeatureDefinition reservedFeature = FeatureDefinition.Create(
            "facilities.gym", "Gym", null, null, "facilities", FeatureCatalogueStatus.Reserved,
            isCore: false, 4, FeatureEntitlementType.Boolean);
        _featureDefinitions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([CoreFeature, NonCoreFeature, reservedFeature]);
        SubscriptionPackageVersionId versionId = SubscriptionPackageVersionId.New();
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(CreateSubscription(versionId));
        // Even a package row that would otherwise enable it must not win over Reserved status.
        _packageFeatures.GetForPackageVersionAsync(versionId, Arg.Any<CancellationToken>())
            .Returns([new SubscriptionPackageFeature(versionId, reservedFeature.Id, enabled: true, limitValue: null)]);
        TenantEntitlementService sut = CreateService();

        IReadOnlyList<EffectiveEntitlement> details = await sut.GetEffectiveEntitlementDetails(TenantId, CancellationToken.None);

        details.Single(e => e.FeatureKey == "facilities.gym").Enabled.Should().BeFalse();
    }
}
