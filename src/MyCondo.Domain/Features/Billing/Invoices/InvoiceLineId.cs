namespace MyCondo.Domain.Features.Billing.Invoices;

public readonly record struct InvoiceLineId(Guid Value)
{
    public static InvoiceLineId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static InvoiceLineId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new InvoiceLineId(g)
            : throw new FormatException($"Invalid InvoiceLineId: '{s}'");
}
