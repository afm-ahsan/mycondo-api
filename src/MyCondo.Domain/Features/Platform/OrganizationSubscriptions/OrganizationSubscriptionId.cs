namespace MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

public readonly record struct OrganizationSubscriptionId(Guid Value)
{
    public static OrganizationSubscriptionId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
