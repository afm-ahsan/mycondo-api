using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Domain.Features.Amenities.PoolIncidents;

public interface IPoolIncidentRepository
{
    Task<PoolIncident?> GetByIdAsync(PoolIncidentId id, CancellationToken cancellationToken);

    Task<PagedResult<PoolIncident>> SearchAsync(
        Guid tenantId, FacilityId? facilityId, int page, int pageSize, CancellationToken cancellationToken);

    void Add(PoolIncident poolIncident);
}
