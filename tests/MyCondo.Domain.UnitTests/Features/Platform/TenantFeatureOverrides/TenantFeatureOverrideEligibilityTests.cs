using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Platform.TenantFeatureOverrides;

public class TenantFeatureOverrideEligibilityTests
{
    private static FeatureDefinition CreateFeature(bool isCore, FeatureCatalogueStatus status) =>
        FeatureDefinition.Create(
            "finance.expenses", "Expenses", null, null, "finance", status, isCore, 1, FeatureEntitlementType.Boolean);

    [Fact]
    public void Validate_Allows_Active_Non_Core_Feature()
    {
        FeatureDefinition feature = CreateFeature(isCore: false, status: FeatureCatalogueStatus.Active);

        Action act = () => TenantFeatureOverrideEligibility.Validate(feature);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Throws_For_Core_Feature()
    {
        FeatureDefinition feature = CreateFeature(isCore: true, status: FeatureCatalogueStatus.Active);

        Action act = () => TenantFeatureOverrideEligibility.Validate(feature);

        act.Should().Throw<CoreFeatureOverrideNotAllowedException>();
    }

    [Fact]
    public void Validate_Throws_For_Reserved_Feature()
    {
        FeatureDefinition feature = CreateFeature(isCore: false, status: FeatureCatalogueStatus.Reserved);

        Action act = () => TenantFeatureOverrideEligibility.Validate(feature);

        act.Should().Throw<ReservedFeatureOverrideNotAllowedException>();
    }
}
