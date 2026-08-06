using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Property.Buildings;

public interface IBuildingRepository
{
    Task<Building?> GetByIdAsync(BuildingId id, CancellationToken cancellationToken);

    Task<Building?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken);

    Task<Building?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken);

    Task<PagedResult<Building>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Building building);
}
