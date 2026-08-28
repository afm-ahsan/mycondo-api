using AwesomeAssertions;
using MyCondo.Application.Common.Authorization;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.UnitTests.Common.Authorization;

public class LegacyTenantModuleFeatureMapTests
{
    [Fact]
    public void Every_TenantModuleKey_Has_Exactly_One_Mapping()
    {
        // The exhaustiveness guarantee Task 04 §32 requires: a future addition to TenantModuleKeys with
        // no deliberate mapping decision must fail this test, not silently disappear from migration.
        LegacyTenantModuleFeatureMap.Mappings.Select(m => m.LegacyModuleKey)
            .Should().BeEquivalentTo(TenantModuleKeys.All);
    }

    [Fact]
    public void Has_No_Duplicate_Legacy_Keys()
    {
        LegacyTenantModuleFeatureMap.Mappings.Select(m => m.LegacyModuleKey)
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetMapping_Throws_For_An_Unmapped_Key()
    {
        Action act = () => LegacyTenantModuleFeatureMap.GetMapping("not-a-real-legacy-key");

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("property")]
    [InlineData("billing")]
    [InlineData("payments")]
    [InlineData("expenses")]
    [InlineData("security")]
    [InlineData("leasing")]
    [InlineData("residents")]
    [InlineData("utilities")]
    [InlineData("amenities")]
    [InlineData("operations")]
    public void Direct_Mappings_Target_A_Real_Active_FeatureCatalogue_Entry(string legacyKey)
    {
        LegacyModuleMapping mapping = LegacyTenantModuleFeatureMap.GetMapping(legacyKey);

        mapping.Kind.Should().Be(LegacyModuleMappingKind.Direct);
        mapping.TargetFeatureKey.Should().NotBeNull();

        (string Key, string Name, string? Description, string? ParentKey, string Module,
            FeatureCatalogueStatus Status, bool IsCore, int DisplayOrder, FeatureEntitlementType EntitlementType) entry =
            FeatureCatalogue.Entries.Single(e => e.Key == mapping.TargetFeatureKey);

        entry.Status.Should().Be(FeatureCatalogueStatus.Active);
        entry.IsCore.Should().Be(mapping.TargetIsCore);
    }

    [Theory]
    [InlineData("vendors")]
    [InlineData("payroll")]
    [InlineData("complaints")]
    [InlineData("notifications")]
    [InlineData("documents")]
    [InlineData("maintenance")]
    public void Reserved_Compatibility_Mappings_Target_A_Reserved_FeatureCatalogue_Entry_With_The_Same_Key(string legacyKey)
    {
        LegacyModuleMapping mapping = LegacyTenantModuleFeatureMap.GetMapping(legacyKey);

        mapping.Kind.Should().Be(LegacyModuleMappingKind.ReservedCompatibility);
        mapping.TargetFeatureKey.Should().Be(legacyKey);

        (string Key, string Name, string? Description, string? ParentKey, string Module,
            FeatureCatalogueStatus Status, bool IsCore, int DisplayOrder, FeatureEntitlementType EntitlementType) entry =
            FeatureCatalogue.Entries.Single(e => e.Key == legacyKey);

        entry.Status.Should().Be(FeatureCatalogueStatus.Reserved);
        entry.IsCore.Should().BeFalse();
    }

    [Fact]
    public void Reporting_Is_Explicitly_Unresolved_With_No_Target()
    {
        LegacyModuleMapping mapping = LegacyTenantModuleFeatureMap.GetMapping("reporting");

        mapping.Kind.Should().Be(LegacyModuleMappingKind.ExplicitlyUnresolved);
        mapping.TargetFeatureKey.Should().BeNull();
    }
}
