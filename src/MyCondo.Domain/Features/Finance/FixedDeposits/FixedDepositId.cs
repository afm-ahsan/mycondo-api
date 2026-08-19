namespace MyCondo.Domain.Features.Finance.FixedDeposits;

public readonly record struct FixedDepositId(Guid Value)
{
    public static FixedDepositId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static FixedDepositId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new FixedDepositId(g)
            : throw new FormatException($"Invalid FixedDepositId: '{s}'");
}
