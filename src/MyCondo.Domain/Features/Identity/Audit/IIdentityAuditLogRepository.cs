namespace MyCondo.Domain.Features.Identity.Audit;

public interface IIdentityAuditLogRepository
{
    void Add(IdentityAuditLogEntry entry);

    Task<IReadOnlyList<IdentityAuditLogEntry>> GetRecentAsync(
        Guid tenantId, int take, CancellationToken cancellationToken);
}
