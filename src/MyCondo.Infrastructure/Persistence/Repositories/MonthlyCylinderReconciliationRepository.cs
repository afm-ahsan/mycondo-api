using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class MonthlyCylinderReconciliationRepository(MyCondoDbContext db) : IMonthlyCylinderReconciliationRepository
{
    public Task<MonthlyCylinderReconciliation?> GetByIdAsync(MonthlyCylinderReconciliationId id, CancellationToken cancellationToken) =>
        db.Set<MonthlyCylinderReconciliation>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<MonthlyCylinderReconciliation>> SearchAsync(
        Guid tenantId, string? cylinderType, int page, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<MonthlyCylinderReconciliation> query = db.Set<MonthlyCylinderReconciliation>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(cylinderType))
        {
            query = query.Where(x => x.CylinderType == cylinderType);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<MonthlyCylinderReconciliation> items = await query
            .OrderByDescending(x => x.PeriodMonth)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MonthlyCylinderReconciliation>(items, page, pageSize, total);
    }

    public void Add(MonthlyCylinderReconciliation reconciliation) => db.Set<MonthlyCylinderReconciliation>().Add(reconciliation);
}
