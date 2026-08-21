namespace MyCondo.Domain.Features.Finance.Audit;

public readonly record struct FinanceAuditLogEntryId(Guid Value)
{
    public static FinanceAuditLogEntryId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
