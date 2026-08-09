namespace MyCondo.Domain.Features.Platform.PlatformAudit;

public readonly record struct PlatformAuditLogEntryId(Guid Value)
{
    public static PlatformAuditLogEntryId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
