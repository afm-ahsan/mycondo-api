using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Domain.UnitTests.Features.Platform.SubscriptionPackages;

public class SubscriptionPackageFeatureTests
{
    [Fact]
    public void Create_Sets_All_Fields()
    {
        SubscriptionPackageVersionId versionId = SubscriptionPackageVersionId.New();
        FeatureDefinitionId featureId = FeatureDefinitionId.New();

        SubscriptionPackageFeature assignment = new(versionId, featureId, enabled: true, limitValue: 5);

        assignment.PackageVersionId.Should().Be(versionId);
        assignment.FeatureId.Should().Be(featureId);
        assignment.Enabled.Should().BeTrue();
        assignment.LimitValue.Should().Be(5);
    }

    [Fact]
    public void Create_Allows_Null_LimitValue()
    {
        SubscriptionPackageFeature assignment = new(
            SubscriptionPackageVersionId.New(), FeatureDefinitionId.New(), enabled: true, limitValue: null);

        assignment.LimitValue.Should().BeNull();
    }

    [Fact]
    public void Create_Throws_For_Negative_LimitValue()
    {
        Action act = () => _ = new SubscriptionPackageFeature(
            SubscriptionPackageVersionId.New(), FeatureDefinitionId.New(), enabled: true, limitValue: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
