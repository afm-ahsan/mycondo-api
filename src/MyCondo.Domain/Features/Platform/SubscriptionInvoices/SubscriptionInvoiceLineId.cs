namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices;

public readonly record struct SubscriptionInvoiceLineId(Guid Value)
{
    public static SubscriptionInvoiceLineId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
