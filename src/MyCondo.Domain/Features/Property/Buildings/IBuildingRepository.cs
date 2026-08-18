using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Property.Buildings;

public interface IBuildingRepository
{
    Task<Building?> GetByIdAsync(BuildingId id, CancellationToken cancellationToken);

    /// <summary>Batched lookup for resolving display names for a set of buildings (e.g. list rows)
    /// without one query per row. Missing ids are simply absent from the result.</summary>
    Task<List<Building>> GetByIdsAsync(IReadOnlyCollection<BuildingId> ids, CancellationToken cancellationToken);

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
