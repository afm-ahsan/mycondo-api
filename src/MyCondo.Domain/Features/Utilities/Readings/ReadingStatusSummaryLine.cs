using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Domain.Features.Utilities.Readings;

/// <summary>Current-snapshot count of readings by (UtilityType, Status) — not date-range scoped, since
/// it reflects the reading pipeline's present state (how much work is pending), not a historical trend.</summary>
public sealed record ReadingStatusSummaryLine(UtilityType UtilityType, ReadingStatus Status, int Count);
