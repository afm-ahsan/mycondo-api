using AwesomeAssertions;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Billing.ServiceChargeRules.Exceptions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.UnitTests.Features.Billing.ServiceChargeRules;

public class ServiceChargeRuleTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    private static ServiceChargeRule CreateRule() =>
        ServiceChargeRule.Create(
            TenantId, BuildingId, "ServiceCharge", "Standard Charge", CalculationMethod.FixedAmount, 1500m, null,
            BillingFrequency.Monthly, EffectiveFrom, Now);

    [Fact]
    public void Create_Starts_Active_With_Version_One_And_No_EffectiveTo()
    {
        ServiceChargeRule rule = CreateRule();

        rule.IsActive.Should().BeTrue();
        rule.EffectiveTo.Should().BeNull();
        rule.Version.Should().Be(1);
    }

    [Fact]
    public void Create_Throws_When_Rate_Is_Not_Positive()
    {
        Action act = () => ServiceChargeRule.Create(
            TenantId, BuildingId, "ServiceCharge", "Standard", CalculationMethod.FixedAmount, 0m, null,
            BillingFrequency.Monthly, EffectiveFrom, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EndEffectivePeriod_Sets_EffectiveTo()
    {
        ServiceChargeRule rule = CreateRule();

        rule.EndEffectivePeriod(new DateOnly(2026, 6, 30));

        rule.EffectiveTo.Should().Be(new DateOnly(2026, 6, 30));
        rule.Version.Should().Be(2);
    }

    [Fact]
    public void EndEffectivePeriod_Throws_When_Already_Ended()
    {
        ServiceChargeRule rule = CreateRule();
        rule.EndEffectivePeriod(new DateOnly(2026, 6, 30));

        Action act = () => rule.EndEffectivePeriod(new DateOnly(2026, 7, 31));

        act.Should().Throw<ServiceChargeRuleAlreadyEndedException>();
    }

    [Fact]
    public void EndEffectivePeriod_Throws_When_EffectiveTo_Precedes_EffectiveFrom()
    {
        ServiceChargeRule rule = CreateRule();

        Action act = () => rule.EndEffectivePeriod(EffectiveFrom.AddDays(-1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deactivate_Sets_IsActive_False()
    {
        ServiceChargeRule rule = CreateRule();

        rule.Deactivate();

        rule.IsActive.Should().BeFalse();
        rule.Version.Should().Be(2);
    }

    [Fact]
    public void AppliesToPeriod_True_When_Period_Fully_Contained_And_Active()
    {
        ServiceChargeRule rule = CreateRule();

        rule.AppliesToPeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)).Should().BeTrue();
    }

    [Fact]
    public void AppliesToPeriod_False_When_Period_Starts_Before_EffectiveFrom()
    {
        ServiceChargeRule rule = CreateRule();

        rule.AppliesToPeriod(EffectiveFrom.AddDays(-1), EffectiveFrom.AddDays(29)).Should().BeFalse();
    }

    [Fact]
    public void AppliesToPeriod_False_When_Period_Extends_Past_EffectiveTo()
    {
        ServiceChargeRule rule = CreateRule();
        rule.EndEffectivePeriod(new DateOnly(2026, 6, 30));

        rule.AppliesToPeriod(new DateOnly(2026, 6, 1), new DateOnly(2026, 7, 1)).Should().BeFalse();
    }

    [Fact]
    public void AppliesToPeriod_False_When_Deactivated_Even_If_Effective_Dates_Cover_Period()
    {
        ServiceChargeRule rule = CreateRule();
        rule.Deactivate();

        rule.AppliesToPeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)).Should().BeFalse();
    }

    [Fact]
    public void Create_Accepts_UnitTypeFilter_And_PerSquareFoot_Method()
    {
        ServiceChargeRule rule = ServiceChargeRule.Create(
            TenantId, BuildingId, "Maintenance", "Sqft Charge", CalculationMethod.PerSquareFoot, 2.5m,
            FlatType.Residential, BillingFrequency.Monthly, EffectiveFrom, Now);

        rule.CalculationMethod.Should().Be(CalculationMethod.PerSquareFoot);
        rule.UnitTypeFilter.Should().Be(FlatType.Residential);
    }
}
