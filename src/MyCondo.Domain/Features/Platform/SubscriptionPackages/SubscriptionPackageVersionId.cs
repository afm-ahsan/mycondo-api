namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

public readonly record struct SubscriptionPackageVersionId(Guid Value)
{
    public static SubscriptionPackageVersionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
