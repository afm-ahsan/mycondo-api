using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Domain.Features.Utilities.Meters;

public interface IMeterRepository
{
    Task<Meter?> GetByIdAsync(MeterId id, CancellationToken cancellationToken);

    Task<Meter?> GetByMeterNumberAsync(
        Guid tenantId, UtilityType utilityType, string meterNumber, CancellationToken cancellationToken);

    Task<PagedResult<Meter>> SearchAsync(
        Guid tenantId, BuildingId buildingId, UtilityType? utilityType, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Current-snapshot COUNT grouped by (UtilityType, Status), tenant-wide (optionally
    /// building-scoped) — unlike <see cref="SearchAsync"/>, buildingId is optional here.</summary>
    Task<IReadOnlyList<MeterStatusSummaryLine>> GetStatusSummaryAsync(
        Guid tenantId, BuildingId? buildingId, UtilityType? utilityType, CancellationToken cancellationToken);

    void Add(Meter meter);
}
