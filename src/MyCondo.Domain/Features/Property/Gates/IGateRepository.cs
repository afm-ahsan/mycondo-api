using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Property.Gates;

public interface IGateRepository
{
    Task<Gate?> GetByIdAsync(GateId id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(
        Guid tenantId, BuildingId buildingId, string code, GateId? excludingId, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        Guid tenantId, BuildingId buildingId, string name, GateId? excludingId, CancellationToken cancellationToken);

    Task<List<Gate>> GetAllForBuildingAsync(
        Guid tenantId, BuildingId buildingId, bool activeOnly, CancellationToken cancellationToken);

    void Add(Gate gate);
}
