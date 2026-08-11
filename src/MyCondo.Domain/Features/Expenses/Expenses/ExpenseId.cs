namespace MyCondo.Domain.Features.Expenses.Expenses;

public readonly record struct ExpenseId(Guid Value)
{
    public static ExpenseId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ExpenseId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ExpenseId(g)
            : throw new FormatException($"Invalid ExpenseId: '{s}'");
}
