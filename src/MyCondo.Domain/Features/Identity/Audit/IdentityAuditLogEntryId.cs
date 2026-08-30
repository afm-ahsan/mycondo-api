namespace MyCondo.Domain.Features.Identity.Audit;

public readonly record struct IdentityAuditLogEntryId(Guid Value)
{
    public static IdentityAuditLogEntryId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
