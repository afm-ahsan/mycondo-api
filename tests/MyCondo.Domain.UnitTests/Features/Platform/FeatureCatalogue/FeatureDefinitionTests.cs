using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.UnitTests.Features.Platform.FeatureCatalogue;

public class FeatureDefinitionTests
{
    [Fact]
    public void Create_Sets_All_Fields_For_A_Group_Root()
    {
        FeatureDefinition feature = FeatureDefinition.Create(
            "finance", "Finance & Accounting", null, null, "finance",
            FeatureCatalogueStatus.Active, isCore: false, displayOrder: 800, FeatureEntitlementType.Boolean);

        feature.Key.Should().Be("finance");
        feature.Name.Should().Be("Finance & Accounting");
        feature.Description.Should().BeNull();
        feature.ParentFeatureId.Should().BeNull();
        feature.Module.Should().Be("finance");
        feature.Status.Should().Be(FeatureCatalogueStatus.Active);
        feature.IsCore.Should().BeFalse();
        feature.DisplayOrder.Should().Be(800);
        feature.EntitlementType.Should().Be(FeatureEntitlementType.Boolean);
    }

    [Fact]
    public void Create_Sets_ParentFeatureId_For_A_Leaf()
    {
        FeatureDefinitionId parentId = FeatureDefinitionId.New();

        FeatureDefinition feature = FeatureDefinition.Create(
            "finance.expenses", "Expenses", null, parentId, "finance",
            FeatureCatalogueStatus.Active, isCore: true, displayOrder: 803, FeatureEntitlementType.Boolean);

        feature.ParentFeatureId.Should().Be(parentId);
        feature.IsCore.Should().BeTrue();
    }

    [Fact]
    public void Create_Trims_Key_Name_Description_And_Module()
    {
        FeatureDefinition feature = FeatureDefinition.Create(
            "  finance  ", "  Finance  ", "  desc  ", null, "  finance  ",
            FeatureCatalogueStatus.Active, isCore: false, displayOrder: 1, FeatureEntitlementType.Boolean);

        feature.Key.Should().Be("finance");
        feature.Name.Should().Be("Finance");
        feature.Description.Should().Be("desc");
        feature.Module.Should().Be("finance");
    }

    [Fact]
    public void Create_Throws_For_Blank_Key()
    {
        Action act = () => FeatureDefinition.Create(
            "  ", "Finance", null, null, "finance",
            FeatureCatalogueStatus.Active, false, 1, FeatureEntitlementType.Boolean);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_For_Blank_Name()
    {
        Action act = () => FeatureDefinition.Create(
            "finance", "  ", null, null, "finance",
            FeatureCatalogueStatus.Active, false, 1, FeatureEntitlementType.Boolean);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_For_Blank_Module()
    {
        Action act = () => FeatureDefinition.Create(
            "finance", "Finance", null, null, "  ",
            FeatureCatalogueStatus.Active, false, 1, FeatureEntitlementType.Boolean);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_For_Negative_DisplayOrder()
    {
        Action act = () => FeatureDefinition.Create(
            "finance", "Finance", null, null, "finance",
            FeatureCatalogueStatus.Active, false, -1, FeatureEntitlementType.Boolean);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Two_Features_With_Different_Ids_Are_Not_Equal()
    {
        FeatureDefinition a = FeatureDefinition.Create(
            "finance", "Finance", null, null, "finance",
            FeatureCatalogueStatus.Active, false, 1, FeatureEntitlementType.Boolean);
        FeatureDefinition b = FeatureDefinition.Create(
            "finance", "Finance", null, null, "finance",
            FeatureCatalogueStatus.Active, false, 1, FeatureEntitlementType.Boolean);

        a.Should().NotBe(b);
        a.Id.Should().NotBe(b.Id);
    }
}
