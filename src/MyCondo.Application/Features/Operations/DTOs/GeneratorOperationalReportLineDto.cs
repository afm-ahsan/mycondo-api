namespace MyCondo.Application.Features.Operations.DTOs;

/// <summary>One line per generator, covering the spec's "Runtime / Fuel usage / Cost per hour"
/// reports (§5.13) in a single query rather than three near-identical ones.</summary>
public sealed record GeneratorOperationalReportLineDto(
    Guid GeneratorId,
    string GeneratorName,
    int SessionCount,
    int TotalRuntimeMinutes,
    decimal TotalFuelConsumed,
    decimal TotalFuelReceived,
    decimal TotalFuelCost)
{
    public decimal? CostPerHour => TotalRuntimeMinutes > 0
        ? Math.Round(TotalFuelCost / (TotalRuntimeMinutes / 60m), 2)
        : null;
}
