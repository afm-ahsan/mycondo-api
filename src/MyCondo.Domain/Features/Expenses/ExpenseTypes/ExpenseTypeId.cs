namespace MyCondo.Domain.Features.Expenses.ExpenseTypes;

public readonly record struct ExpenseTypeId(Guid Value)
{
    public static ExpenseTypeId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ExpenseTypeId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ExpenseTypeId(g)
            : throw new FormatException($"Invalid ExpenseTypeId: '{s}'");
}
