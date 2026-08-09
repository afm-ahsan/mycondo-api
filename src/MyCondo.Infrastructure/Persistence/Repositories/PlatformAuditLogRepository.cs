using MyCondo.Domain.Features.Platform.PlatformAudit;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PlatformAuditLogRepository(MyCondoDbContext db) : IPlatformAuditLogRepository
{
    public void Add(PlatformAuditLogEntry entry) => db.Set<PlatformAuditLogEntry>().Add(entry);
}
