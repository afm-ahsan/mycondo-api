namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

/// <summary>ADR-033 §7 — package root lifecycle, distinct from <see cref="SubscriptionPackageVersionStatus"/>
/// (a version's own effective-dated lifecycle) and the future <c>OrganizationSubscription</c> lifecycle
/// (a tenant's commercial agreement). These three state machines are never collapsed into one.</summary>
public enum SubscriptionPackageStatus
{
    Draft = 0,
    Active = 1,
    Retired = 2
}
