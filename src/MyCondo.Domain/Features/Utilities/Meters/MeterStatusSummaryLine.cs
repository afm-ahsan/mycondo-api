using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Domain.Features.Utilities.Meters;

/// <summary>Current-snapshot count of meters by (UtilityType, Status) — the meter fleet's health at
/// this moment, not date-range scoped.</summary>
public sealed record MeterStatusSummaryLine(UtilityType UtilityType, MeterStatus Status, int Count);
