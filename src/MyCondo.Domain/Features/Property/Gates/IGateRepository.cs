using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Property.Gates;

public interface IGateRepository
{
    Task<Gate?> GetByIdAsync(GateId id, CancellationToken cancellationToken);

    Task<Gate?> GetByNameAsync(
        Guid tenantId, BuildingId buildingId, string name, CancellationToken cancellationToken);

    Task<List<Gate>> GetAllForBuildingAsync(
        Guid tenantId, BuildingId buildingId, CancellationToken cancellationToken);

    void Add(Gate gate);
}
