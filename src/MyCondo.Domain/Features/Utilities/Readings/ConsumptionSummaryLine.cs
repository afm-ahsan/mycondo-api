using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Domain.Features.Utilities.Readings;

/// <summary>One UtilityType's consumption total over a period, counted only from authoritative
/// (Finalized or Billed) readings — Draft/Reviewed are not yet accepted, and Corrected readings are
/// superseded by the reading that corrects them.</summary>
public sealed record ConsumptionSummaryLine(UtilityType UtilityType, decimal TotalConsumptionUnits, int ReadingCount);
