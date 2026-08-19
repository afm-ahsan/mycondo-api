namespace MyCondo.Domain.Features.Finance.ChartOfAccounts;

public readonly record struct ChartOfAccountId(Guid Value)
{
    public static ChartOfAccountId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ChartOfAccountId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ChartOfAccountId(g)
            : throw new FormatException($"Invalid ChartOfAccountId: '{s}'");
}
