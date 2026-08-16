using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Gates;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class GateRepository(MyCondoDbContext db) : IGateRepository
{
    public Task<Gate?> GetByIdAsync(GateId id, CancellationToken cancellationToken) =>
        db.Set<Gate>().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(
        Guid tenantId, BuildingId buildingId, string code, GateId? excludingId, CancellationToken cancellationToken) =>
        db.Set<Gate>().AnyAsync(
            g => g.TenantId == tenantId && g.BuildingId == buildingId && g.Code == code
                && (excludingId == null || g.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> ExistsByNameAsync(
        Guid tenantId, BuildingId buildingId, string name, GateId? excludingId, CancellationToken cancellationToken) =>
        db.Set<Gate>().AnyAsync(
            g => g.TenantId == tenantId && g.BuildingId == buildingId && g.Name == name
                && (excludingId == null || g.Id != excludingId.Value),
            cancellationToken);

    public Task<List<Gate>> GetAllForBuildingAsync(
        Guid tenantId, BuildingId buildingId, bool activeOnly, CancellationToken cancellationToken)
    {
        IQueryable<Gate> query = db.Set<Gate>()
            .Where(g => g.TenantId == tenantId && g.BuildingId == buildingId);

        if (activeOnly)
        {
            query = query.Where(g => g.IsActive);
        }

        return query.OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name).ToListAsync(cancellationToken);
    }

    public void Add(Gate gate) => db.Set<Gate>().Add(gate);
}
