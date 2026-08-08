using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Domain.Features.Utilities.Readings;

public interface IReadingRepository
{
    Task<Reading?> GetByIdAsync(ReadingId id, CancellationToken cancellationToken);

    Task<PagedResult<Reading>> SearchAsync(
        Guid tenantId, MeterId? meterId, FlatId? flatId, ReadingStatus? status, int page, int pageSize,
        CancellationToken cancellationToken);

    /// <summary>The most recent Finalized-or-later (Finalized/Billed) reading for the meter, ordered
    /// by <see cref="Reading.PeriodEnd"/> — used to validate a new reading's PreviousReading matches
    /// continuity, and as the base for the abnormal-consumption average.</summary>
    Task<Reading?> GetLastFinalizedAsync(Guid tenantId, MeterId meterId, CancellationToken cancellationToken);

    /// <summary>Up to <paramref name="count"/> most recent Finalized-or-later readings for the meter,
    /// most recent first — feeds the abnormal-consumption average heuristic.</summary>
    Task<IReadOnlyList<Reading>> GetRecentFinalizedAsync(
        Guid tenantId, MeterId meterId, int count, CancellationToken cancellationToken);

    /// <summary>True if a non-Corrected reading already exists for this (meter, period) — the
    /// application-layer pre-check ahead of the DB partial unique index.</summary>
    Task<bool> ExistsActiveForMeterAndPeriodAsync(
        Guid tenantId, MeterId meterId, DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken);

    Task<IReadOnlyList<Reading>> GetConsumptionHistoryAsync(
        Guid tenantId, MeterId meterId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>SUM(ConsumptionUnits)/COUNT grouped by UtilityType, for authoritative (Finalized or
    /// Billed) readings whose PeriodEnd falls in [fromDate, toDate].</summary>
    Task<IReadOnlyList<ConsumptionSummaryLine>> GetConsumptionSummaryAsync(
        Guid tenantId, BuildingId? buildingId, UtilityType? utilityType, DateOnly fromDate, DateOnly toDate,
        CancellationToken cancellationToken);

    /// <summary>Current-snapshot COUNT grouped by (UtilityType, Status), across every reading status —
    /// this one is not restricted to Finalized/Billed, since its purpose is showing how much work is
    /// sitting in each stage of the pipeline.</summary>
    Task<IReadOnlyList<ReadingStatusSummaryLine>> GetStatusSummaryAsync(
        Guid tenantId, BuildingId? buildingId, UtilityType? utilityType, CancellationToken cancellationToken);

    void Add(Reading reading);
}
