using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Finance.Audit;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class FinanceAuditLogRepository(MyCondoDbContext db) : IFinanceAuditLogRepository
{
    public void Add(FinanceAuditLogEntry entry) => db.Set<FinanceAuditLogEntry>().Add(entry);

    public async Task<IReadOnlyList<FinanceAuditLogEntry>> GetRecentAsync(
        Guid tenantId, int take, CancellationToken cancellationToken) =>
        await db.Set<FinanceAuditLogEntry>()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
}
