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

    void Add(Meter meter);
}
