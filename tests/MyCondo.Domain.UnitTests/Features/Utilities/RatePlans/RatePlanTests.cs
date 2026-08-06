using AwesomeAssertions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.RatePlans;
using MyCondo.Domain.Features.Utilities.RatePlans.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Utilities.RatePlans;

public class RatePlanTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    private static readonly RateSlabInput[] ValidSlabs =
    [
        new(1, 0m, 100m, 5m),
        new(2, 100m, 300m, 7m),
        new(3, 300m, null, 9m),
    ];

    private static (RatePlan Plan, IReadOnlyList<RateSlab> Slabs) CreateMeteredPlan() =>
        RatePlan.Create(
            TenantId, BuildingId, UtilityType.Electricity, "Standard Electricity", RateStructure.Metered, null,
            50m, 5m, EffectiveFrom, ValidSlabs, Now);

    [Fact]
    public void Create_Metered_Produces_Active_Plan_With_Slabs()
    {
        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = CreateMeteredPlan();

        plan.IsActive.Should().BeTrue();
        plan.EffectiveTo.Should().BeNull();
        plan.Version.Should().Be(1);
        slabs.Should().HaveCount(3);
        slabs.Should().OnlyContain(s => s.RatePlanId == plan.Id);
    }

    [Fact]
    public void Create_Fixed_Requires_FixedAmount_And_No_Slabs()
    {
        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = RatePlan.Create(
            TenantId, BuildingId, UtilityType.Gas, "Flat Gas Charge", RateStructure.Fixed, 800m, 0m, 0m,
            EffectiveFrom, [], Now);

        plan.Structure.Should().Be(RateStructure.Fixed);
        plan.FixedAmount.Should().Be(800m);
        slabs.Should().BeEmpty();
    }

    [Fact]
    public void Create_Fixed_Throws_When_FixedAmount_Missing()
    {
        Action act = () => RatePlan.Create(
            TenantId, BuildingId, UtilityType.Gas, "Flat Gas Charge", RateStructure.Fixed, null, 0m, 0m,
            EffectiveFrom, [], Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Fixed_Throws_When_Slabs_Provided()
    {
        Action act = () => RatePlan.Create(
            TenantId, BuildingId, UtilityType.Gas, "Flat Gas Charge", RateStructure.Fixed, 800m, 0m, 0m,
            EffectiveFrom, ValidSlabs, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Metered_Throws_When_No_Slabs()
    {
        Action act = () => RatePlan.Create(
            TenantId, BuildingId, UtilityType.Electricity, "Standard Electricity", RateStructure.Metered, null,
            50m, 5m, EffectiveFrom, [], Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Metered_Throws_When_First_Slab_Does_Not_Start_At_Zero()
    {
        RateSlabInput[] slabs = [new(1, 10m, null, 5m)];

        Action act = () => RatePlan.Create(
            TenantId, BuildingId, UtilityType.Electricity, "Standard Electricity", RateStructure.Metered, null,
            50m, 5m, EffectiveFrom, slabs, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Metered_Throws_When_Slabs_Are_Not_Contiguous()
    {
        RateSlabInput[] slabs = [new(1, 0m, 100m, 5m), new(2, 150m, null, 7m)];

        Action act = () => RatePlan.Create(
            TenantId, BuildingId, UtilityType.Electricity, "Standard Electricity", RateStructure.Metered, null,
            50m, 5m, EffectiveFrom, slabs, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Metered_Throws_When_A_NonLast_Slab_Is_Unbounded()
    {
        RateSlabInput[] slabs = [new(1, 0m, null, 5m), new(2, 100m, null, 7m)];

        Action act = () => RatePlan.Create(
            TenantId, BuildingId, UtilityType.Electricity, "Standard Electricity", RateStructure.Metered, null,
            50m, 5m, EffectiveFrom, slabs, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EndEffectivePeriod_Sets_EffectiveTo()
    {
        (RatePlan plan, _) = CreateMeteredPlan();

        plan.EndEffectivePeriod(new DateOnly(2026, 12, 31));

        plan.EffectiveTo.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void EndEffectivePeriod_Throws_When_Already_Ended()
    {
        (RatePlan plan, _) = CreateMeteredPlan();
        plan.EndEffectivePeriod(new DateOnly(2026, 12, 31));

        Action act = () => plan.EndEffectivePeriod(new DateOnly(2027, 1, 31));

        act.Should().Throw<RatePlanAlreadyEndedException>();
    }

    [Fact]
    public void AppliesToPeriod_False_When_Deactivated()
    {
        (RatePlan plan, _) = CreateMeteredPlan();
        plan.Deactivate();

        plan.AppliesToPeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)).Should().BeFalse();
    }

    [Fact]
    public void AppliesToPeriod_True_When_Period_Fully_Contained()
    {
        (RatePlan plan, _) = CreateMeteredPlan();

        plan.AppliesToPeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)).Should().BeTrue();
    }
}
