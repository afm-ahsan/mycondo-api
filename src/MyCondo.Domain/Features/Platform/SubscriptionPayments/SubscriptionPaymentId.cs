namespace MyCondo.Domain.Features.Platform.SubscriptionPayments;

public readonly record struct SubscriptionPaymentId(Guid Value)
{
    public static SubscriptionPaymentId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
