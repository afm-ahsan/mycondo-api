using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Platform.SubscriptionPackages;

public class SubscriptionPackageFeatureCompositionTests
{
    private static readonly SubscriptionPackageVersionId PackageVersionId = SubscriptionPackageVersionId.New();

    private static FeatureDefinition Group(string key) =>
        FeatureDefinition.Create(key, key, null, null, "finance", FeatureCatalogueStatus.Active, false, 1, FeatureEntitlementType.Boolean);

    private static FeatureDefinition Leaf(string key, FeatureDefinitionId parentId, FeatureEntitlementType entitlementType = FeatureEntitlementType.Boolean) =>
        FeatureDefinition.Create(key, key, null, parentId, "finance", FeatureCatalogueStatus.Active, false, 2, entitlementType);

    [Fact]
    public void Validate_Accepts_Leaf_Enabled_With_Parent_Also_Enabled()
    {
        FeatureDefinition group = Group("finance");
        FeatureDefinition leaf = Leaf("finance.expenses", group.Id);
        List<FeatureDefinition> catalogue = [group, leaf];

        List<SubscriptionPackageFeature> assignments =
        [
            new(PackageVersionId, group.Id, enabled: true, limitValue: null),
            new(PackageVersionId, leaf.Id, enabled: true, limitValue: null),
        ];

        Action act = () => SubscriptionPackageFeatureComposition.Validate(PackageVersionId, catalogue, assignments);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Rejects_Leaf_Enabled_Without_Parent_Enabled_In_Same_Version()
    {
        FeatureDefinition group = Group("finance");
        FeatureDefinition leaf = Leaf("finance.expenses", group.Id);
        List<FeatureDefinition> catalogue = [group, leaf];

        List<SubscriptionPackageFeature> assignments =
        [
            new(PackageVersionId, group.Id, enabled: false, limitValue: null),
            new(PackageVersionId, leaf.Id, enabled: true, limitValue: null),
        ];

        Action act = () => SubscriptionPackageFeatureComposition.Validate(PackageVersionId, catalogue, assignments);

        act.Should().Throw<MissingParentFeatureAssignmentException>();
    }

    [Fact]
    public void Validate_Rejects_Leaf_Enabled_Without_Any_Parent_Row_At_All()
    {
        FeatureDefinition group = Group("finance");
        FeatureDefinition leaf = Leaf("finance.expenses", group.Id);
        List<FeatureDefinition> catalogue = [group, leaf];

        List<SubscriptionPackageFeature> assignments = [new(PackageVersionId, leaf.Id, enabled: true, limitValue: null)];

        Action act = () => SubscriptionPackageFeatureComposition.Validate(PackageVersionId, catalogue, assignments);

        act.Should().Throw<MissingParentFeatureAssignmentException>();
    }

    [Fact]
    public void Validate_Allows_Parent_Enabled_Without_Implicitly_Enabling_Children()
    {
        FeatureDefinition group = Group("finance");
        FeatureDefinition leaf = Leaf("finance.expenses", group.Id);
        List<FeatureDefinition> catalogue = [group, leaf];

        List<SubscriptionPackageFeature> assignments =
        [
            new(PackageVersionId, group.Id, enabled: true, limitValue: null),
            new(PackageVersionId, leaf.Id, enabled: false, limitValue: null),
        ];

        Action act = () => SubscriptionPackageFeatureComposition.Validate(PackageVersionId, catalogue, assignments);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Rejects_Duplicate_Feature_Assignment()
    {
        FeatureDefinition group = Group("finance");
        List<FeatureDefinition> catalogue = [group];

        List<SubscriptionPackageFeature> assignments =
        [
            new(PackageVersionId, group.Id, enabled: true, limitValue: null),
            new(PackageVersionId, group.Id, enabled: false, limitValue: null),
        ];

        Action act = () => SubscriptionPackageFeatureComposition.Validate(PackageVersionId, catalogue, assignments);

        act.Should().Throw<DuplicatePackageFeatureAssignmentException>();
    }

    [Fact]
    public void Validate_Rejects_Unknown_Feature_Reference()
    {
        List<FeatureDefinition> catalogue = [];
        List<SubscriptionPackageFeature> assignments =
            [new SubscriptionPackageFeature(PackageVersionId, FeatureDefinitionId.New(), enabled: true, limitValue: null)];

        Action act = () => SubscriptionPackageFeatureComposition.Validate(PackageVersionId, catalogue, assignments);

        act.Should().Throw<UnknownPackageFeatureException>();
    }

    [Fact]
    public void Validate_Rejects_LimitValue_On_A_Boolean_Entitlement_Feature()
    {
        FeatureDefinition feature = Group("finance");
        List<FeatureDefinition> catalogue = [feature];
        List<SubscriptionPackageFeature> assignments =
            [new SubscriptionPackageFeature(PackageVersionId, feature.Id, enabled: true, limitValue: 10)];

        Action act = () => SubscriptionPackageFeatureComposition.Validate(PackageVersionId, catalogue, assignments);

        act.Should().Throw<InvalidPackageFeatureLimitValueException>();
    }

    [Fact]
    public void Validate_Allows_LimitValue_On_A_Numeric_Entitlement_Feature()
    {
        FeatureDefinition feature = FeatureDefinition.Create(
            "limit.buildings", "Buildings", null, null, "administration",
            FeatureCatalogueStatus.Active, false, 1, FeatureEntitlementType.Numeric);
        List<FeatureDefinition> catalogue = [feature];
        List<SubscriptionPackageFeature> assignments =
            [new SubscriptionPackageFeature(PackageVersionId, feature.Id, enabled: true, limitValue: 10)];

        Action act = () => SubscriptionPackageFeatureComposition.Validate(PackageVersionId, catalogue, assignments);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Walks_Multi_Level_Parent_Chain()
    {
        FeatureDefinition root = Group("finance");
        FeatureDefinition mid = Leaf("finance.reports", root.Id);
        FeatureDefinition leaf = Leaf("finance.reports.export", mid.Id);
        List<FeatureDefinition> catalogue = [root, mid, leaf];

        List<SubscriptionPackageFeature> assignments =
        [
            new(PackageVersionId, root.Id, enabled: true, limitValue: null),
            new(PackageVersionId, leaf.Id, enabled: true, limitValue: null),
        ];

        Action act = () => SubscriptionPackageFeatureComposition.Validate(PackageVersionId, catalogue, assignments);

        act.Should().Throw<MissingParentFeatureAssignmentException>();
    }
}
