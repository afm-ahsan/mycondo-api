using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Property.Flats;

public interface IFlatRepository
{
    Task<Flat?> GetByIdAsync(FlatId id, CancellationToken cancellationToken);

    /// <summary>Batched lookup for resolving display names for a set of flats (e.g. list rows) without
    /// one query per row. Missing ids are simply absent from the result.</summary>
    Task<List<Flat>> GetByIdsAsync(IReadOnlyCollection<FlatId> ids, CancellationToken cancellationToken);

    Task<Flat?> GetByFlatNumberAsync(
        Guid tenantId, BuildingId buildingId, string flatNumber, CancellationToken cancellationToken);

    Task<PagedResult<Flat>> SearchAsync(
        Guid tenantId,
        BuildingId buildingId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Tenant-wide search backing the global Administration flat directory — <paramref name="buildingId"/>
    /// filters to one building when supplied, otherwise every building in the tenant is searched.</summary>
    Task<PagedResult<Flat>> SearchForTenantAsync(
        Guid tenantId,
        BuildingId? buildingId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Flats a PerSquareFoot service-charge rule cannot bill yet — see billing readiness check.</summary>
    Task<PagedResult<Flat>> SearchMissingAreaSqFtAsync(
        Guid tenantId,
        BuildingId buildingId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Unpaginated — invoice batch generation needs every flat in the building, not a page.</summary>
    Task<IReadOnlyList<Flat>> GetAllForBuildingAsync(Guid tenantId, BuildingId buildingId, CancellationToken cancellationToken);

    void Add(Flat flat);
}
