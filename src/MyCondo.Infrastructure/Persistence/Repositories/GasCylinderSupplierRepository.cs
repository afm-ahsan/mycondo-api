using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class GasCylinderSupplierRepository(MyCondoDbContext db) : IGasCylinderSupplierRepository
{
    public Task<GasCylinderSupplier?> GetByIdAsync(GasCylinderSupplierId id, CancellationToken cancellationToken) =>
        db.Set<GasCylinderSupplier>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<GasCylinderSupplier>> SearchAsync(
        Guid tenantId, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<GasCylinderSupplier> query = db.Set<GasCylinderSupplier>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        long total = await query.LongCountAsync(cancellationToken);

        List<GasCylinderSupplier> items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<GasCylinderSupplier>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<GasCylinderSupplier>> ListActiveAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Set<GasCylinderSupplier>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .ToListAsync(cancellationToken);

    public void Add(GasCylinderSupplier supplier) => db.Set<GasCylinderSupplier>().Add(supplier);
}
