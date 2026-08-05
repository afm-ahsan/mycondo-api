using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Property.Flats;

public interface IFlatRepository
{
    Task<Flat?> GetByIdAsync(FlatId id, CancellationToken cancellationToken);

    Task<Flat?> GetByFlatNumberAsync(
        Guid tenantId, BuildingId buildingId, string flatNumber, CancellationToken cancellationToken);

    Task<PagedResult<Flat>> SearchAsync(
        Guid tenantId,
        BuildingId buildingId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Flat flat);
}
