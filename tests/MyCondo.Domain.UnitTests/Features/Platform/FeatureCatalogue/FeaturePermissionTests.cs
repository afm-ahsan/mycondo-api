using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.UnitTests.Features.Platform.FeatureCatalogue;

public class FeaturePermissionTests
{
    [Fact]
    public void Constructor_Sets_FeatureId_And_PermissionCode()
    {
        FeatureDefinitionId featureId = FeatureDefinitionId.New();

        FeaturePermission mapping = new(featureId, "expense.view");

        mapping.FeatureId.Should().Be(featureId);
        mapping.PermissionCode.Should().Be("expense.view");
    }

    [Fact]
    public void Constructor_Trims_PermissionCode()
    {
        FeaturePermission mapping = new(FeatureDefinitionId.New(), "  expense.view  ");

        mapping.PermissionCode.Should().Be("expense.view");
    }

    [Fact]
    public void Constructor_Throws_For_Blank_PermissionCode()
    {
        Action act = () => _ = new FeaturePermission(FeatureDefinitionId.New(), "  ");

        act.Should().Throw<ArgumentException>();
    }
}
