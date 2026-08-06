namespace MyCondo.Domain.Features.Payments.Ledger;

public readonly record struct LedgerEntryId(Guid Value)
{
    public static LedgerEntryId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static LedgerEntryId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new LedgerEntryId(g)
            : throw new FormatException($"Invalid LedgerEntryId: '{s}'");
}
