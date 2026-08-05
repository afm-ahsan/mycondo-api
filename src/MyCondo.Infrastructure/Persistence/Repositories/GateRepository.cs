using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Gates;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class GateRepository(MyCondoDbContext db) : IGateRepository
{
    public Task<Gate?> GetByIdAsync(GateId id, CancellationToken cancellationToken) =>
        db.Set<Gate>().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<Gate?> GetByNameAsync(
        Guid tenantId, BuildingId buildingId, string name, CancellationToken cancellationToken) =>
        db.Set<Gate>().FirstOrDefaultAsync(
            g => g.TenantId == tenantId && g.BuildingId == buildingId && g.Name == name,
            cancellationToken);

    public Task<List<Gate>> GetAllForBuildingAsync(
        Guid tenantId, BuildingId buildingId, CancellationToken cancellationToken) =>
        db.Set<Gate>()
            .Where(g => g.TenantId == tenantId && g.BuildingId == buildingId)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    public void Add(Gate gate) => db.Set<Gate>().Add(gate);
}
