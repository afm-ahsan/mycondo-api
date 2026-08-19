namespace MyCondo.Domain.Features.Finance.FixedDeposits;

public readonly record struct FixedDepositInterestReceiptId(Guid Value)
{
    public static FixedDepositInterestReceiptId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static FixedDepositInterestReceiptId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new FixedDepositInterestReceiptId(g)
            : throw new FormatException($"Invalid FixedDepositInterestReceiptId: '{s}'");
}
