using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class CylinderStockMovementRepository(MyCondoDbContext db) : ICylinderStockMovementRepository
{
    public Task<CylinderStockMovement?> GetByIdAsync(CylinderStockMovementId id, CancellationToken cancellationToken) =>
        db.Set<CylinderStockMovement>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<CylinderStockMovement>> SearchAsync(
        Guid tenantId, string? cylinderType, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<CylinderStockMovement> query = db.Set<CylinderStockMovement>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(cylinderType))
        {
            query = query.Where(x => x.CylinderType == cylinderType);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<CylinderStockMovement> items = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CylinderStockMovement>(items, page, pageSize, total);
    }

    public async Task<int> GetCurrentStockAsync(Guid tenantId, string cylinderType, CancellationToken cancellationToken) =>
        await db.Set<CylinderStockMovement>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CylinderType == cylinderType)
            .SumAsync(x => x.Quantity, cancellationToken);

    public async Task<IReadOnlyList<string>> ListDistinctCylinderTypesAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Set<CylinderStockMovement>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.CylinderType)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CylinderStockMovement>> GetForPeriodAsync(
        Guid tenantId, string cylinderType, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        await db.Set<CylinderStockMovement>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CylinderType == cylinderType
                && x.OccurredAtUtc >= fromUtc && x.OccurredAtUtc <= toUtc)
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(CylinderStockMovement movement) => db.Set<CylinderStockMovement>().Add(movement);
}
