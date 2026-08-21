namespace MyCondo.Domain.Features.Expenses.ExpenseCategories;

public readonly record struct ExpenseCategoryId(Guid Value)
{
    public static ExpenseCategoryId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ExpenseCategoryId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ExpenseCategoryId(g)
            : throw new FormatException($"Invalid ExpenseCategoryId: '{s}'");
}
