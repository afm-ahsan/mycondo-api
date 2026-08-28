using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Platform.TenantFeatureOverrides;

public class TenantFeatureOverrideTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CreatedBy = Guid.NewGuid();
    private static readonly DateTimeOffset EffectiveFrom = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static FeatureDefinition CreateFeature(bool isCore = false, FeatureCatalogueStatus status = FeatureCatalogueStatus.Active) =>
        FeatureDefinition.Create(
            "facilities.swimming_pool", "Swimming Pool", null, null, "facilities", status, isCore, 1,
            FeatureEntitlementType.Boolean);

    private static TenantFeatureOverride CreateOverride(
        FeatureDefinition? feature = null,
        bool enabled = true,
        DateTimeOffset? effectiveUntil = null,
        string? reason = "Pilot enterprise customer") =>
        TenantFeatureOverride.Create(
            TenantId, feature ?? CreateFeature(), enabled, EffectiveFrom, effectiveUntil, reason, CreatedBy, EffectiveFrom);

    [Fact]
    public void Create_Enable_Override_Sets_All_Fields()
    {
        FeatureDefinition feature = CreateFeature();

        TenantFeatureOverride @override = CreateOverride(feature: feature, enabled: true);

        @override.TenantId.Should().Be(TenantId);
        @override.FeatureId.Should().Be(feature.Id);
        @override.Enabled.Should().BeTrue();
        @override.EffectiveFrom.Should().Be(EffectiveFrom);
        @override.EffectiveUntil.Should().BeNull();
        @override.Reason.Should().Be("Pilot enterprise customer");
        @override.CreatedBy.Should().Be(CreatedBy);
        @override.CreatedAt.Should().Be(EffectiveFrom);
    }

    [Fact]
    public void Create_Disable_Override_Sets_Enabled_False()
    {
        TenantFeatureOverride @override = CreateOverride(enabled: false);

        @override.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Create_Throws_For_Empty_TenantId()
    {
        Action act = () => TenantFeatureOverride.Create(
            Guid.Empty, CreateFeature(), true, EffectiveFrom, null, "reason", CreatedBy, EffectiveFrom);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_When_EffectiveUntil_Before_EffectiveFrom()
    {
        Action act = () => CreateOverride(effectiveUntil: EffectiveFrom.AddDays(-1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Allows_EffectiveUntil_Equal_To_EffectiveFrom()
    {
        TenantFeatureOverride @override = CreateOverride(effectiveUntil: EffectiveFrom);

        @override.EffectiveUntil.Should().Be(EffectiveFrom);
    }

    [Fact]
    public void Create_Throws_For_Empty_CreatedBy()
    {
        Action act = () => TenantFeatureOverride.Create(
            TenantId, CreateFeature(), true, EffectiveFrom, null, "reason", Guid.Empty, EffectiveFrom);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Allows_Null_Reason()
    {
        TenantFeatureOverride @override = CreateOverride(reason: null);

        @override.Reason.Should().BeNull();
    }

    [Fact]
    public void Create_Throws_For_Whitespace_Only_Reason()
    {
        Action act = () => CreateOverride(reason: "   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Trims_Reason()
    {
        TenantFeatureOverride @override = CreateOverride(reason: "  needs trimming  ");

        @override.Reason.Should().Be("needs trimming");
    }

    [Fact]
    public void Create_Throws_For_Core_Feature()
    {
        FeatureDefinition coreFeature = CreateFeature(isCore: true);

        Action act = () => CreateOverride(feature: coreFeature);

        act.Should().Throw<CoreFeatureOverrideNotAllowedException>();
    }

    [Fact]
    public void Create_Throws_For_Reserved_Feature()
    {
        FeatureDefinition reservedFeature = CreateFeature(status: FeatureCatalogueStatus.Reserved);

        Action act = () => CreateOverride(feature: reservedFeature);

        act.Should().Throw<ReservedFeatureOverrideNotAllowedException>();
    }

    [Fact]
    public void Two_Overrides_With_Different_Ids_Are_Not_Equal()
    {
        TenantFeatureOverride a = CreateOverride();
        TenantFeatureOverride b = CreateOverride();

        a.Should().NotBe(b);
    }
}
