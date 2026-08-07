namespace MyCondo.Application.Features.Operations.DTOs;

public sealed record GeneratorSessionDto(
    Guid GeneratorSessionId,
    Guid GeneratorId,
    DateTimeOffset StartAtUtc,
    DateTimeOffset? StopAtUtc,
    Guid? OperatorId,
    decimal OpeningFuelLevel,
    decimal? ClosingFuelLevel,
    string? OutageReason,
    int? RuntimeMinutes,
    string Status)
{
    /// <summary>Presentation-only convenience — not stored, derived from already-authoritative fields.</summary>
    public decimal? FuelConsumed => ClosingFuelLevel is decimal closing ? OpeningFuelLevel - closing : null;
}
