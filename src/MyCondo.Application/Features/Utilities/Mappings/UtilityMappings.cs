using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Domain.Features.Utilities.MeterAssignments;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.RatePlans;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Mappings;

internal static class UtilityMappings
{
    public static MeterDto ToDto(this Meter meter) => new(
        meter.Id.Value, meter.BuildingId.Value, meter.UtilityType.ToString(), meter.MeterNumber, meter.Status.ToString(),
        meter.ReplacesMeterId?.Value);

    public static MeterAssignmentDto ToDto(this MeterAssignment assignment) => new(
        assignment.Id.Value, assignment.MeterId.Value, assignment.FlatId.Value, assignment.AssignedFromUtc,
        assignment.AssignedToUtc);

    public static RateSlabDto ToDto(this RateSlab slab) => new(slab.SlabOrder, slab.FromUnits, slab.ToUnits, slab.RatePerUnit);

    public static RatePlanDto ToDto(this RatePlan plan, IReadOnlyList<RateSlab> slabs) => new(
        plan.Id.Value, plan.BuildingId.Value, plan.UtilityType.ToString(), plan.Name, plan.Structure.ToString(),
        plan.FixedAmount, plan.FixedServiceCharge, plan.TaxPercentage, plan.EffectiveFrom, plan.EffectiveTo,
        plan.IsActive, slabs.Select(s => s.ToDto()).ToList());

    public static ReadingDto ToDto(this Reading reading) => new(
        reading.Id.Value, reading.MeterId.Value, reading.FlatId.Value, reading.UtilityType.ToString(),
        reading.BuildingId.Value, reading.PeriodStart, reading.PeriodEnd, reading.PreviousReading,
        reading.PresentReading, reading.ConsumptionUnits, reading.ReadingDate, reading.OverrideReason,
        reading.IsAbnormalConsumption, reading.AbnormalConsumptionReason, reading.Status.ToString(),
        reading.ReviewedAtUtc, reading.ReviewedBy, reading.FinalizedAtUtc, reading.FinalizedBy, reading.BilledAtUtc,
        reading.BilledBy, reading.InvoiceId?.Value, reading.CorrectsReadingId?.Value);
}
