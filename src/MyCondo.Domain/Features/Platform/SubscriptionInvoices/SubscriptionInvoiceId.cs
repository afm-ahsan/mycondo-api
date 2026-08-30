namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices;

public readonly record struct SubscriptionInvoiceId(Guid Value)
{
    public static SubscriptionInvoiceId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
