namespace MyCondo.Domain.Features.Billing.Invoices;

public readonly record struct InvoiceId(Guid Value)
{
    public static InvoiceId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static InvoiceId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new InvoiceId(g)
            : throw new FormatException($"Invalid InvoiceId: '{s}'");
}
