using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
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

    void Add(Reading reading);
}
