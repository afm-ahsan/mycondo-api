using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Domain.UnitTests.Features.Platform.FeatureCatalogue;

/// <summary>Pure-function precedence tests for the ADR-033 §13 resolution algorithm (Task 05 §39).</summary>
public class TenantEntitlementResolutionTests
{
    private static readonly SubscriptionPackageVersionId VersionId = SubscriptionPackageVersionId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static FeatureDefinition Core(string key = "property") =>
        FeatureDefinition.Create(key, key, null, null, "property", FeatureCatalogueStatus.Active, isCore: true, 1, FeatureEntitlementType.Boolean);

    private static FeatureDefinition NonCore(string key = "finance.expenses", FeatureEntitlementType type = FeatureEntitlementType.Boolean) =>
        FeatureDefinition.Create(key, key, null, null, "finance", FeatureCatalogueStatus.Active, isCore: false, 1, type);

    private static FeatureDefinition Reserved(string key = "vendors") =>
        FeatureDefinition.Create(key, key, null, null, "vendors", FeatureCatalogueStatus.Reserved, isCore: false, 1, FeatureEntitlementType.Boolean);

    private static TenantFeatureOverride Override(FeatureDefinition feature, bool enabled) =>
        TenantFeatureOverride.Create(
            Guid.NewGuid(), feature, enabled, effectiveFrom: Now.AddDays(-1), effectiveUntil: null,
            reason: "test", createdBy: Guid.NewGuid(), createdAtUtc: Now.AddDays(-1));

    // --- Core ---

    [Fact]
    public void Core_Feature_Is_Enabled_With_No_Subscription_No_Override_No_Package()
    {
        FeatureDefinition feature = Core();

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: false, null, null);

        result.Enabled.Should().BeTrue();
        result.Source.Should().Be(EntitlementSource.Core);
    }

    [Fact]
    public void Core_Feature_Wins_Over_A_State_That_Would_Otherwise_Disable_It()
    {
        // No subscription would disable any non-core feature (step 3) — Core short-circuits before that.
        FeatureDefinition feature = Core();

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: false, null,
            new SubscriptionPackageFeature(VersionId, feature.Id, enabled: false, limitValue: null));

        result.Enabled.Should().BeTrue();
        result.Source.Should().Be(EntitlementSource.Core);
    }

    // --- Reserved ---

    [Fact]
    public void Reserved_Feature_Is_Disabled_Even_When_A_Package_Row_Enables_It()
    {
        FeatureDefinition feature = Reserved();
        SubscriptionPackageFeature packageFeature = new(VersionId, feature.Id, enabled: true, limitValue: null);

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: true, null, packageFeature);

        result.Enabled.Should().BeFalse();
        result.Source.Should().Be(EntitlementSource.Reserved);
    }

    // --- No subscription ---

    [Fact]
    public void NonCore_Feature_Is_Disabled_With_No_Subscription_Even_If_An_Override_Would_Enable_It()
    {
        // ADR-033 §13 step 3 (no subscription) short-circuits before step 4 (override) is ever consulted.
        FeatureDefinition feature = NonCore();
        TenantFeatureOverride enablingOverride = Override(feature, enabled: true);

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: false, enablingOverride, null);

        result.Enabled.Should().BeFalse();
        result.Source.Should().Be(EntitlementSource.DefaultDisabled);
    }

    // --- Override ---

    [Fact]
    public void Enabling_Override_Wins_Over_Package_Absence()
    {
        FeatureDefinition feature = NonCore();
        TenantFeatureOverride enablingOverride = Override(feature, enabled: true);

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: true, enablingOverride, packageFeature: null);

        result.Enabled.Should().BeTrue();
        result.Source.Should().Be(EntitlementSource.TenantOverrideEnabled);
    }

    [Fact]
    public void Disabling_Override_Wins_Over_A_Package_Grant()
    {
        FeatureDefinition feature = NonCore();
        TenantFeatureOverride disablingOverride = Override(feature, enabled: false);
        SubscriptionPackageFeature packageFeature = new(VersionId, feature.Id, enabled: true, limitValue: null);

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: true, disablingOverride, packageFeature);

        result.Enabled.Should().BeFalse();
        result.Source.Should().Be(EntitlementSource.TenantOverrideDisabled);
    }

    // --- Package ---

    [Fact]
    public void Package_Feature_Enabled_Row_Is_Honored()
    {
        FeatureDefinition feature = NonCore();
        SubscriptionPackageFeature packageFeature = new(VersionId, feature.Id, enabled: true, limitValue: null);

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: true, null, packageFeature);

        result.Enabled.Should().BeTrue();
        result.Source.Should().Be(EntitlementSource.Package);
    }

    [Fact]
    public void Absent_Package_Feature_Row_Is_Disabled()
    {
        FeatureDefinition feature = NonCore();

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: true, null, packageFeature: null);

        result.Enabled.Should().BeFalse();
        result.Source.Should().Be(EntitlementSource.DefaultDisabled);
    }

    // --- Default ---

    [Fact]
    public void NonCore_No_Override_No_Package_Is_Disabled()
    {
        FeatureDefinition feature = NonCore();

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: true, null, null);

        result.Enabled.Should().BeFalse();
        result.Source.Should().Be(EntitlementSource.DefaultDisabled);
    }

    // --- Numeric ---

    [Fact]
    public void Numeric_Package_Feature_Returns_Its_LimitValue_When_Enabled()
    {
        FeatureDefinition feature = NonCore("limit.buildings", FeatureEntitlementType.Numeric);
        SubscriptionPackageFeature packageFeature = new(VersionId, feature.Id, enabled: true, limitValue: 10);

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: true, null, packageFeature);

        result.Enabled.Should().BeTrue();
        result.LimitValue.Should().Be(10);
    }

    [Fact]
    public void Numeric_Package_Feature_LimitValue_Is_Null_When_The_Row_Is_Disabled()
    {
        FeatureDefinition feature = NonCore("limit.buildings", FeatureEntitlementType.Numeric);
        SubscriptionPackageFeature packageFeature = new(VersionId, feature.Id, enabled: false, limitValue: 10);

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: true, null, packageFeature);

        result.Enabled.Should().BeFalse();
        result.LimitValue.Should().BeNull();
    }

    [Fact]
    public void An_Override_On_A_Numeric_Feature_Carries_No_LimitValue()
    {
        // TenantFeatureOverride has no limit field to carry (ADR-033 §18) — overriding a Numeric feature
        // can only toggle Enabled, never adjust the limit.
        FeatureDefinition feature = NonCore("limit.buildings", FeatureEntitlementType.Numeric);
        TenantFeatureOverride enablingOverride = Override(feature, enabled: true);
        SubscriptionPackageFeature packageFeature = new(VersionId, feature.Id, enabled: true, limitValue: 10);

        EffectiveEntitlement result = TenantEntitlementResolution.Resolve(feature, hasCurrentSubscription: true, enablingOverride, packageFeature);

        result.Enabled.Should().BeTrue();
        result.LimitValue.Should().BeNull();
    }
}
