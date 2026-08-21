namespace MyCondo.Domain.Features.Finance.AccountingPeriods;

public readonly record struct AccountingPeriodId(Guid Value)
{
    public static AccountingPeriodId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static AccountingPeriodId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new AccountingPeriodId(g)
            : throw new FormatException($"Invalid AccountingPeriodId: '{s}'");
}
