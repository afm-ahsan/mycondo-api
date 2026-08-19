namespace MyCondo.Domain.Features.Finance.FixedDeposits;

public readonly record struct FixedDepositInterestAccrualId(Guid Value)
{
    public static FixedDepositInterestAccrualId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static FixedDepositInterestAccrualId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new FixedDepositInterestAccrualId(g)
            : throw new FormatException($"Invalid FixedDepositInterestAccrualId: '{s}'");
}
