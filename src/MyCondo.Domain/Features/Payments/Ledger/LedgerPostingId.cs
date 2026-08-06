namespace MyCondo.Domain.Features.Payments.Ledger;

public readonly record struct LedgerPostingId(Guid Value)
{
    public static LedgerPostingId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static LedgerPostingId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new LedgerPostingId(g)
            : throw new FormatException($"Invalid LedgerPostingId: '{s}'");
}
