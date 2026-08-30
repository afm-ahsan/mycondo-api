using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Domain.Features.Utilities.Meters;

public interface IMeterRepository
{
    Task<Meter?> GetByIdAsync(MeterId id, CancellationToken cancellationToken);

    /// <summary>Thin projection backing ADR-033 Task 09A resource-derived feature resolution — returns
    /// null (never the meter) when the id does not exist or is not owned by <paramref name="tenantId"/>,
    /// so a cross-tenant id yields the same not-found outcome as a genuinely missing one.</summary>
    Task<UtilityType?> GetUtilityTypeAsync(Guid tenantId, MeterId id, CancellationToken cancellationToken);

    Task<Meter?> GetByMeterNumberAsync(
        Guid tenantId, UtilityType utilityType, string meterNumber, CancellationToken cancellationToken);

    Task<PagedResult<Meter>> SearchAsync(
        Guid tenantId, BuildingId? buildingId, UtilityType? utilityType, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Current-snapshot COUNT grouped by (UtilityType, Status), tenant-wide (optionally
    /// building-scoped) — unlike <see cref="SearchAsync"/>, buildingId is optional here.</summary>
    Task<IReadOnlyList<MeterStatusSummaryLine>> GetStatusSummaryAsync(
        Guid tenantId, BuildingId? buildingId, UtilityType? utilityType, CancellationToken cancellationToken);

    void Add(Meter meter);
}
