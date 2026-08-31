using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.Audit;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class IdentityAuditLogRepository(MyCondoDbContext db) : IIdentityAuditLogRepository
{
    public void Add(IdentityAuditLogEntry entry) => db.Set<IdentityAuditLogEntry>().Add(entry);

    public async Task<IReadOnlyList<IdentityAuditLogEntry>> GetRecentAsync(
        Guid tenantId, int take, CancellationToken cancellationToken) =>
        await db.Set<IdentityAuditLogEntry>()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
}
