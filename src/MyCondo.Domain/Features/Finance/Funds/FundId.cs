namespace MyCondo.Domain.Features.Finance.Funds;

public readonly record struct FundId(Guid Value)
{
    public static FundId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static FundId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new FundId(g)
            : throw new FormatException($"Invalid FundId: '{s}'");
}
