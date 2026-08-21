namespace MyCondo.Domain.Features.Finance.FinancialAccounts;

public readonly record struct FinancialAccountId(Guid Value)
{
    public static FinancialAccountId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static FinancialAccountId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new FinancialAccountId(g)
            : throw new FormatException($"Invalid FinancialAccountId: '{s}'");
}
