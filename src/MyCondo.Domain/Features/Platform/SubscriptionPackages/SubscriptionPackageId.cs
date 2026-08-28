namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

public readonly record struct SubscriptionPackageId(Guid Value)
{
    public static SubscriptionPackageId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
