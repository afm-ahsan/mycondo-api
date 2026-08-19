namespace MyCondo.Domain.Features.Finance.FinancialYears;

public readonly record struct FinancialYearId(Guid Value)
{
    public static FinancialYearId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static FinancialYearId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new FinancialYearId(g)
            : throw new FormatException($"Invalid FinancialYearId: '{s}'");
}
