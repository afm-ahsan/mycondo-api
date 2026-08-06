using AwesomeAssertions;
using MyCondo.Application.Features.Utilities.Services;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.RatePlans;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.UnitTests.Features.Utilities.Services;

public class UtilityCalculatorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly MeterId MeterId = MeterId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateOnly PeriodStart = new(2026, 3, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 3, 31);

    private static readonly RateSlabInput[] Slabs =
    [
        new(1, 0m, 100m, 5m),
        new(2, 100m, 300m, 7m),
        new(3, 300m, null, 9m),
    ];

    private static Reading CreateReading(decimal consumptionPresent) =>
        Reading.Record(
            TenantId, MeterId, FlatId, UtilityType.Electricity, BuildingId, PeriodStart, PeriodEnd, 0m,
            consumptionPresent, PeriodEnd, null, null, Now);

    private static (RatePlan Plan, IReadOnlyList<RateSlab> Slabs) FixedPlan(decimal amount) =>
        RatePlan.Create(
            TenantId, BuildingId, UtilityType.Gas, "Flat Gas Charge", RateStructure.Fixed, amount, 0m, 0m,
            PeriodStart, [], Now);

    private static (RatePlan Plan, IReadOnlyList<RateSlab> Slabs) MeteredPlan(
        decimal fixedServiceCharge = 0m, decimal taxPercentage = 0m) =>
        RatePlan.Create(
            TenantId, BuildingId, UtilityType.Electricity, "Standard Electricity", RateStructure.Metered, null,
            fixedServiceCharge, taxPercentage, PeriodStart, Slabs, Now);

    [Fact]
    public void Fixed_Structure_Returns_FixedAmount_Regardless_Of_Consumption()
    {
        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = FixedPlan(800m);
        Reading reading = CreateReading(500m);

        InvoiceLineInput line = UtilityCalculator.Calculate(plan, slabs, reading);

        line.LineAmount.Should().Be(800m);
        line.CalculationMethodSnapshot.Should().Be("Fixed");
    }

    [Fact]
    public void Metered_Single_Slab_Charges_Rate_Times_Units()
    {
        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = MeteredPlan();
        Reading reading = CreateReading(50m); // within first slab (0-100 @ 5)

        InvoiceLineInput line = UtilityCalculator.Calculate(plan, slabs, reading);

        line.LineAmount.Should().Be(250m); // 50 * 5
    }

    [Fact]
    public void Metered_Multiple_Slabs_Charges_Each_Tier_At_Its_Own_Rate()
    {
        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = MeteredPlan();
        Reading reading = CreateReading(350m); // 100@5 + 200@7 + 50@9

        InvoiceLineInput line = UtilityCalculator.Calculate(plan, slabs, reading);

        // 100*5=500, 200*7=1400, 50*9=450 => 2350
        line.LineAmount.Should().Be(2350m);
    }

    [Fact]
    public void Metered_Adds_FixedServiceCharge_On_Top_Of_Slab_Charges()
    {
        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = MeteredPlan(fixedServiceCharge: 50m);
        Reading reading = CreateReading(50m); // 50*5=250

        InvoiceLineInput line = UtilityCalculator.Calculate(plan, slabs, reading);

        line.LineAmount.Should().Be(300m); // 250 + 50 service charge
    }

    [Fact]
    public void Metered_Applies_Tax_Percentage_On_Top_Of_Subtotal()
    {
        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = MeteredPlan(taxPercentage: 10m);
        Reading reading = CreateReading(50m); // subtotal 250, tax 25

        InvoiceLineInput line = UtilityCalculator.Calculate(plan, slabs, reading);

        line.LineAmount.Should().Be(275m);
    }

    [Fact]
    public void Metered_Treats_Negative_Consumption_As_Zero_Units_Never_A_Negative_Charge()
    {
        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = MeteredPlan(fixedServiceCharge: 50m);
        Reading reading = Reading.Record(
            TenantId, MeterId, FlatId, UtilityType.Electricity, BuildingId, PeriodStart, PeriodEnd, 500m, 100m,
            PeriodEnd, "Meter replaced mid-cycle", null, Now);

        InvoiceLineInput line = UtilityCalculator.Calculate(plan, slabs, reading);

        line.LineAmount.Should().Be(50m); // only the fixed service charge, no negative slab charge
    }

    [Fact]
    public void Calculate_Snapshots_Plan_Name_And_UtilityType_Onto_The_Line()
    {
        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = MeteredPlan();
        Reading reading = CreateReading(50m);

        InvoiceLineInput line = UtilityCalculator.Calculate(plan, slabs, reading);

        line.RuleNameSnapshot.Should().Be(plan.Name);
        line.RuleCategorySnapshot.Should().Be(reading.UtilityType.ToString());
        line.ServiceChargeRuleId.Should().BeNull();
    }
}
