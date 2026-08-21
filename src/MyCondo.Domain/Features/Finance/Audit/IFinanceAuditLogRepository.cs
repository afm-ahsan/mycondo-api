namespace MyCondo.Domain.Features.Finance.Audit;

public interface IFinanceAuditLogRepository
{
    void Add(FinanceAuditLogEntry entry);

    Task<IReadOnlyList<FinanceAuditLogEntry>> GetRecentAsync(
        Guid tenantId, int take, CancellationToken cancellationToken);
}
