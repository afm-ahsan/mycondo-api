using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Utilities.RatePlans;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Services;

/// <summary>
/// Computes one <see cref="InvoiceLineInput"/> from a <see cref="RatePlan"/> and a <see cref="Reading"/>
/// — the Application-layer mirror of <c>Billing.Services.ServiceChargeCalculator</c>. Fixed structure
/// returns <see cref="RatePlan.FixedAmount"/> outright; Metered structure walks the plan's
/// <see cref="RateSlab"/>s against <see cref="Reading.ConsumptionUnits"/>, adds
/// <see cref="RatePlan.FixedServiceCharge"/>, then applies <see cref="RatePlan.TaxPercentage"/>.
/// Rounds to 2 decimal places, away from zero — same convention as Slice E. A reading with negative
/// consumption (the override-for-a-lower-present-reading scenario) is billed as zero consumption
/// units, never a negative charge — the override exists to permit recording the anomaly, not to
/// credit the resident automatically.
/// </summary>
public static class UtilityCalculator
{
    public static InvoiceLineInput Calculate(RatePlan plan, IReadOnlyList<RateSlab> slabs, Reading reading)
    {
        decimal preTaxAmount;
        string description;

        if (plan.Structure == RateStructure.Fixed)
        {
            preTaxAmount = plan.FixedAmount!.Value;
            description = $"{plan.Name} ({reading.UtilityType}): fixed charge";
        }
        else
        {
            decimal remainingUnits = Math.Max(reading.ConsumptionUnits, 0m);
            decimal slabCharge = 0m;

            foreach (RateSlab slab in slabs.OrderBy(s => s.SlabOrder))
            {
                if (remainingUnits <= 0)
                {
                    break;
                }

                decimal slabCapacity = slab.ToUnits.HasValue ? slab.ToUnits.Value - slab.FromUnits : remainingUnits;
                decimal unitsInSlab = Math.Min(remainingUnits, slabCapacity);
                slabCharge += unitsInSlab * slab.RatePerUnit;
                remainingUnits -= unitsInSlab;
            }

            preTaxAmount = slabCharge + plan.FixedServiceCharge;
            description = $"{plan.Name} ({reading.UtilityType}): {reading.ConsumptionUnits:0.###} units";
        }

        decimal tax = Math.Round(preTaxAmount * plan.TaxPercentage / 100m, 2, MidpointRounding.AwayFromZero);
        decimal total = Math.Round(preTaxAmount + tax, 2, MidpointRounding.AwayFromZero);

        return new InvoiceLineInput(
            null, plan.Name, reading.UtilityType.ToString(), plan.Structure.ToString(), preTaxAmount, null, 1m,
            total, description);
    }
}
