using AwesomeAssertions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Application.UnitTests.Common.Services;

public class LegacyEntitlementShadowComparerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly SubscriptionPackageVersionId VersionId = SubscriptionPackageVersionId.New();

    private static (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
        FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) BuildCatalogue()
    {
        FeatureDefinition securityRoot = FeatureDefinition.Create(
            "security", "Security", null, null, "security", FeatureCatalogueStatus.Active, isCore: false, 400, FeatureEntitlementType.Boolean);
        FeatureDefinition securityGates = FeatureDefinition.Create(
            "security.gates", "Gates", null, securityRoot.Id, "security", FeatureCatalogueStatus.Active, isCore: false, 401, FeatureEntitlementType.Boolean);
        FeatureDefinition securityVisitors = FeatureDefinition.Create(
            "security.visitors", "Visitors", null, securityRoot.Id, "security", FeatureCatalogueStatus.Active, isCore: false, 402, FeatureEntitlementType.Boolean);

        FeatureDefinition adminRoot = FeatureDefinition.Create(
            "administration", "Administration", null, null, "administration", FeatureCatalogueStatus.Active, isCore: true, 900, FeatureEntitlementType.Boolean);
        FeatureDefinition adminUsers = FeatureDefinition.Create(
            "administration.users", "Users", null, adminRoot.Id, "administration", FeatureCatalogueStatus.Active, isCore: true, 901, FeatureEntitlementType.Boolean);

        FeatureDefinition vendors = FeatureDefinition.Create(
            "vendors", "Vendors (Reserved)", null, null, "vendors", FeatureCatalogueStatus.Reserved, isCore: false, 990, FeatureEntitlementType.Boolean);

        return (securityRoot, securityGates, securityVisitors, adminRoot, adminUsers, vendors);
    }

    [Fact]
    public void Core_Features_Are_Always_Expected_And_Effective_With_No_Legacy_Modules_And_No_Package()
    {
        (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
            FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) catalogue = BuildCatalogue();
        List<FeatureDefinition> all = [catalogue.SecurityRoot, catalogue.SecurityGates, catalogue.SecurityVisitors, catalogue.AdminRoot, catalogue.AdminUsers, catalogue.Vendors];

        ShadowComparisonResult result = LegacyEntitlementShadowComparer.Compare(
            TenantId, enabledLegacyModuleKeys: [], all, assignedPackageFeatures: [], tenantOverrides: [], Now);

        result.LegacyExpectedFeatureKeys.Should().BeEquivalentTo(["administration", "administration.users"]);
        result.NewEffectiveFeatureKeys.Should().BeEquivalentTo(["administration", "administration.users"]);
        result.HasLoss.Should().BeFalse();
    }

    [Fact]
    public void Direct_Mapping_To_A_Group_Root_Expands_To_The_Full_Active_Subtree()
    {
        (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
            FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) catalogue = BuildCatalogue();
        List<FeatureDefinition> all = [catalogue.SecurityRoot, catalogue.SecurityGates, catalogue.SecurityVisitors, catalogue.AdminRoot, catalogue.AdminUsers, catalogue.Vendors];

        ShadowComparisonResult result = LegacyEntitlementShadowComparer.Compare(
            TenantId, enabledLegacyModuleKeys: ["security"], all, assignedPackageFeatures: [], tenantOverrides: [], Now);

        result.LegacyExpectedFeatureKeys.Should().Contain(["security", "security.gates", "security.visitors"]);
    }

    [Fact]
    public void Reserved_And_Unresolved_Legacy_Keys_Contribute_No_Legacy_Expected_Feature()
    {
        (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
            FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) catalogue = BuildCatalogue();
        List<FeatureDefinition> all = [catalogue.SecurityRoot, catalogue.SecurityGates, catalogue.SecurityVisitors, catalogue.AdminRoot, catalogue.AdminUsers, catalogue.Vendors];

        ShadowComparisonResult result = LegacyEntitlementShadowComparer.Compare(
            TenantId, enabledLegacyModuleKeys: ["vendors", "reporting"], all, assignedPackageFeatures: [], tenantOverrides: [], Now);

        result.LegacyExpectedFeatureKeys.Should().BeEquivalentTo(["administration", "administration.users"]);
    }

    [Fact]
    public void A_Package_Enabling_The_Full_Legacy_Expected_Set_Produces_No_Loss()
    {
        (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
            FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) catalogue = BuildCatalogue();
        List<FeatureDefinition> all = [catalogue.SecurityRoot, catalogue.SecurityGates, catalogue.SecurityVisitors, catalogue.AdminRoot, catalogue.AdminUsers, catalogue.Vendors];
        List<SubscriptionPackageFeature> packageFeatures =
        [
            new(VersionId, catalogue.SecurityRoot.Id, enabled: true, limitValue: null),
            new(VersionId, catalogue.SecurityGates.Id, enabled: true, limitValue: null),
            new(VersionId, catalogue.SecurityVisitors.Id, enabled: true, limitValue: null),
        ];

        ShadowComparisonResult result = LegacyEntitlementShadowComparer.Compare(
            TenantId, enabledLegacyModuleKeys: ["security"], all, packageFeatures, tenantOverrides: [], Now);

        result.HasLoss.Should().BeFalse();
        result.LostFeatureKeys.Should().BeEmpty();
    }

    [Fact]
    public void An_Empty_Package_Against_A_Legacy_Enabled_Module_Is_Detected_As_A_Loss()
    {
        (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
            FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) catalogue = BuildCatalogue();
        List<FeatureDefinition> all = [catalogue.SecurityRoot, catalogue.SecurityGates, catalogue.SecurityVisitors, catalogue.AdminRoot, catalogue.AdminUsers, catalogue.Vendors];

        ShadowComparisonResult result = LegacyEntitlementShadowComparer.Compare(
            TenantId, enabledLegacyModuleKeys: ["security"], all, assignedPackageFeatures: [], tenantOverrides: [], Now);

        result.HasLoss.Should().BeTrue();
        result.LostFeatureKeys.Should().Contain(["security", "security.gates", "security.visitors"]);
    }

    [Fact]
    public void An_Effective_Override_Wins_Over_A_Disabled_Package_Row()
    {
        (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
            FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) catalogue = BuildCatalogue();
        List<FeatureDefinition> all = [catalogue.SecurityRoot, catalogue.SecurityGates, catalogue.SecurityVisitors, catalogue.AdminRoot, catalogue.AdminUsers, catalogue.Vendors];
        List<SubscriptionPackageFeature> packageFeatures =
        [
            new(VersionId, catalogue.SecurityGates.Id, enabled: false, limitValue: null),
        ];
        TenantFeatureOverride overrideRow = TenantFeatureOverride.Create(
            TenantId, catalogue.SecurityGates, enabled: true, effectiveFrom: Now.AddDays(-1), effectiveUntil: null,
            reason: "pilot", createdBy: Guid.NewGuid(), createdAtUtc: Now.AddDays(-1));

        ShadowComparisonResult result = LegacyEntitlementShadowComparer.Compare(
            TenantId, enabledLegacyModuleKeys: [], all, packageFeatures, tenantOverrides: [overrideRow], Now);

        result.NewEffectiveFeatureKeys.Should().Contain("security.gates");
    }

    [Fact]
    public void An_Override_Outside_Its_Effective_Window_Is_Ignored()
    {
        (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
            FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) catalogue = BuildCatalogue();
        List<FeatureDefinition> all = [catalogue.SecurityRoot, catalogue.SecurityGates, catalogue.SecurityVisitors, catalogue.AdminRoot, catalogue.AdminUsers, catalogue.Vendors];
        TenantFeatureOverride expiredOverride = TenantFeatureOverride.Create(
            TenantId, catalogue.SecurityGates, enabled: true, effectiveFrom: Now.AddDays(-10),
            effectiveUntil: Now.AddDays(-1), reason: "expired pilot", createdBy: Guid.NewGuid(), createdAtUtc: Now.AddDays(-10));

        ShadowComparisonResult result = LegacyEntitlementShadowComparer.Compare(
            TenantId, enabledLegacyModuleKeys: [], all, assignedPackageFeatures: [], tenantOverrides: [expiredOverride], Now);

        result.NewEffectiveFeatureKeys.Should().NotContain("security.gates");
    }

    [Fact]
    public void A_Reserved_Feature_Is_Never_New_Effective_Even_With_An_Enabled_Package_Row()
    {
        // TenantEntitlementResolution's defensive Reserved guard (Task 05) — nothing at the persistence
        // layer stops a Reserved feature from acquiring an Enabled=true SubscriptionPackageFeature row.
        (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
            FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) catalogue = BuildCatalogue();
        List<FeatureDefinition> all = [catalogue.SecurityRoot, catalogue.SecurityGates, catalogue.SecurityVisitors, catalogue.AdminRoot, catalogue.AdminUsers, catalogue.Vendors];
        List<SubscriptionPackageFeature> packageFeatures = [new(VersionId, catalogue.Vendors.Id, enabled: true, limitValue: null)];

        ShadowComparisonResult result = LegacyEntitlementShadowComparer.Compare(
            TenantId, enabledLegacyModuleKeys: [], all, packageFeatures, tenantOverrides: [], Now);

        result.NewEffectiveFeatureKeys.Should().NotContain("vendors");
    }

    [Fact]
    public void CompareAgainstResolver_Detects_No_Loss_When_The_Live_Service_Grants_The_Full_Legacy_Expected_Set()
    {
        (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
            FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) catalogue = BuildCatalogue();
        List<FeatureDefinition> all = [catalogue.SecurityRoot, catalogue.SecurityGates, catalogue.SecurityVisitors, catalogue.AdminRoot, catalogue.AdminUsers, catalogue.Vendors];
        Dictionary<string, bool> actual = new(StringComparer.Ordinal)
        {
            ["security"] = true,
            ["security.gates"] = true,
            ["security.visitors"] = true,
            ["administration"] = true,
            ["administration.users"] = true,
            ["vendors"] = false,
        };

        ShadowComparisonResult result = LegacyEntitlementShadowComparer.CompareAgainstResolver(
            TenantId, enabledLegacyModuleKeys: ["security"], all, actual);

        result.HasLoss.Should().BeFalse();
    }

    [Fact]
    public void CompareAgainstResolver_Detects_A_Loss_When_The_Live_Service_Disagrees_With_Legacy_Expected_Access()
    {
        (FeatureDefinition SecurityRoot, FeatureDefinition SecurityGates, FeatureDefinition SecurityVisitors,
            FeatureDefinition AdminRoot, FeatureDefinition AdminUsers, FeatureDefinition Vendors) catalogue = BuildCatalogue();
        List<FeatureDefinition> all = [catalogue.SecurityRoot, catalogue.SecurityGates, catalogue.SecurityVisitors, catalogue.AdminRoot, catalogue.AdminUsers, catalogue.Vendors];
        Dictionary<string, bool> actual = new(StringComparer.Ordinal)
        {
            ["security"] = true,
            ["security.gates"] = false,
            ["security.visitors"] = true,
            ["administration"] = true,
            ["administration.users"] = true,
            ["vendors"] = false,
        };

        ShadowComparisonResult result = LegacyEntitlementShadowComparer.CompareAgainstResolver(
            TenantId, enabledLegacyModuleKeys: ["security"], all, actual);

        result.HasLoss.Should().BeTrue();
        result.LostFeatureKeys.Should().Contain("security.gates");
    }
}
