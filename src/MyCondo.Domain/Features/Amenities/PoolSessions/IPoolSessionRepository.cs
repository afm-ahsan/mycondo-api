using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Amenities.PoolSessions;

public interface IPoolSessionRepository
{
    Task<PoolSession?> GetByIdAsync(PoolSessionId id, CancellationToken cancellationToken);

    Task<int> CountOpenAsync(Guid tenantId, FacilityId facilityId, CancellationToken cancellationToken);

    Task<PoolSession?> GetOpenForAccompanimentAsync(
        Guid tenantId, FacilityId facilityId, FlatId flatId, CancellationToken cancellationToken);

    /// <summary>Sessions whose <see cref="PoolSession.EntryAtUtc"/> falls in <c>[fromUtc, toUtc)</c> —
    /// backs the daily usage report. Cross-midnight sessions are counted on their entry day only, per
    /// the report's simple daily scope.</summary>
    Task<IReadOnlyList<PoolSession>> GetForDateAsync(
        Guid tenantId, FacilityId facilityId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    Task<PagedResult<PoolSession>> SearchAsync(
        Guid tenantId, FacilityId? facilityId, FlatId? flatId, bool? openOnly, int page, int pageSize,
        CancellationToken cancellationToken);

    void Add(PoolSession poolSession);
}
