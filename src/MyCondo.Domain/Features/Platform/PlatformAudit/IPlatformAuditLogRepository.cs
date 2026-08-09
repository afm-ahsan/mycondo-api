namespace MyCondo.Domain.Features.Platform.PlatformAudit;

public interface IPlatformAuditLogRepository
{
    void Add(PlatformAuditLogEntry entry);
}
