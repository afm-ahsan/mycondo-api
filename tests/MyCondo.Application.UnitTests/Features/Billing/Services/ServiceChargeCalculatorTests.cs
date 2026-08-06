using AwesomeAssertions;
using MyCondo.Application.Features.Billing.Services;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.UnitTests.Features.Billing.Services;

public class ServiceChargeCalculatorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    private static ServiceChargeRule FixedRule(decimal rate = 1500m) =>
        ServiceChargeRule.Create(
            TenantId, BuildingId, "ServiceCharge", "Standard Charge", CalculationMethod.FixedAmount, rate, null,
            BillingFrequency.Monthly, EffectiveFrom, Now);

    private static ServiceChargeRule PerSqftRule(decimal rate = 2.5m) =>
        ServiceChargeRule.Create(
            TenantId, BuildingId, "Maintenance", "Sqft Charge", CalculationMethod.PerSquareFoot, rate, null,
            BillingFrequency.Monthly, EffectiveFrom, Now);

    private static Flat CreateFlat(decimal? areaSqFt)
    {
        Flat flat = Flat.Create(TenantId, BuildingId, "A-101", 1, FlatType.Residential, Now);
        if (areaSqFt is not null)
        {
            flat.SetAreaSqFt(areaSqFt);
        }

        return flat;
    }

    [Fact]
    public void FixedAmount_Returns_Rate_As_LineAmount()
    {
        ServiceChargeCalculationResult result = ServiceChargeCalculator.Calculate(FixedRule(1500m), CreateFlat(null));

        result.IsSuccess.Should().BeTrue();
        result.Line!.LineAmount.Should().Be(1500m);
        result.Line.Quantity.Should().Be(1m);
        result.Line.AreaSqFtSnapshot.Should().BeNull();
    }

    [Fact]
    public void PerSquareFoot_Multiplies_Rate_By_Area()
    {
        ServiceChargeCalculationResult result = ServiceChargeCalculator.Calculate(PerSqftRule(2.5m), CreateFlat(1000m));

        result.IsSuccess.Should().BeTrue();
        result.Line!.LineAmount.Should().Be(2500m);
        result.Line.AreaSqFtSnapshot.Should().Be(1000m);
    }

    [Fact]
    public void PerSquareFoot_Rounds_To_Two_Decimal_Places_Away_From_Zero()
    {
        ServiceChargeCalculationResult result = ServiceChargeCalculator.Calculate(PerSqftRule(1.005m), CreateFlat(3m));

        // 1.005 * 3 = 3.015 -> rounds to 3.02 (away from zero)
        result.IsSuccess.Should().BeTrue();
        result.Line!.LineAmount.Should().Be(3.02m);
    }

    [Fact]
    public void PerSquareFoot_Skips_When_Flat_Has_No_AreaSqFt()
    {
        ServiceChargeCalculationResult result = ServiceChargeCalculator.Calculate(PerSqftRule(), CreateFlat(null));

        result.IsSuccess.Should().BeFalse();
        result.Line.Should().BeNull();
        result.SkipReason.Should().Contain("AreaSqFt");
    }

    [Fact]
    public void Calculate_Snapshots_Rule_Fields_Onto_The_Line()
    {
        ServiceChargeRule rule = FixedRule(1500m);

        ServiceChargeCalculationResult result = ServiceChargeCalculator.Calculate(rule, CreateFlat(null));

        result.Line!.ServiceChargeRuleId.Should().Be(rule.Id);
        result.Line.RuleNameSnapshot.Should().Be(rule.Name);
        result.Line.RuleCategorySnapshot.Should().Be(rule.Category);
        result.Line.CalculationMethodSnapshot.Should().Be(rule.CalculationMethod.ToString());
        result.Line.RateSnapshot.Should().Be(rule.Rate);
    }
}
