namespace MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;

public readonly record struct GeneratorFuelReceiptId(Guid Value)
{
    public static GeneratorFuelReceiptId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static GeneratorFuelReceiptId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new GeneratorFuelReceiptId(g)
            : throw new FormatException($"Invalid GeneratorFuelReceiptId: '{s}'");
}
