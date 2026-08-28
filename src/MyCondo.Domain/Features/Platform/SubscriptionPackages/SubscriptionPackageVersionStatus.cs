namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

/// <summary>ADR-033 §8 — a single package version's own effective-dated lifecycle. "Active" here means
/// "the current commercially offered version of its package," never "an org's subscription is active"
/// (that is <c>OrganizationSubscription.Status</c>, a separate future concept).</summary>
public enum SubscriptionPackageVersionStatus
{
    Draft = 0,
    Active = 1,
    Superseded = 2
}
