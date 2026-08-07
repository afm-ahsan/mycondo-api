using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.CylinderPurchases;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class CylinderPurchaseRepository(MyCondoDbContext db) : ICylinderPurchaseRepository
{
    public Task<CylinderPurchase?> GetByIdAsync(CylinderPurchaseId id, CancellationToken cancellationToken) =>
        db.Set<CylinderPurchase>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<CylinderPurchase>> SearchAsync(
        Guid tenantId, GasCylinderSupplierId? supplierId, CylinderPurchaseApprovalStatus? approvalStatus, int page,
        int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<CylinderPurchase> query = db.Set<CylinderPurchase>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (supplierId is not null)
        {
            query = query.Where(x => x.SupplierId == supplierId);
        }

        if (approvalStatus is not null)
        {
            query = query.Where(x => x.ApprovalStatus == approvalStatus);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<CylinderPurchase> items = await query
            .OrderByDescending(x => x.PurchaseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CylinderPurchase>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<CylinderPurchase>> GetForPeriodAsync(
        Guid tenantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        await db.Set<CylinderPurchase>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PurchaseDate >= fromDate && x.PurchaseDate <= toDate)
            .OrderBy(x => x.PurchaseDate)
            .ToListAsync(cancellationToken);

    public void Add(CylinderPurchase purchase) => db.Set<CylinderPurchase>().Add(purchase);
}
