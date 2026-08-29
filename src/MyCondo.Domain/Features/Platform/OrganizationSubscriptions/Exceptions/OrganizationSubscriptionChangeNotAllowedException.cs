using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;

/// <summary>Guards <see cref="OrganizationSubscription.ChangePackageVersion"/> (ADR-033 Task 13B) —
/// only an Active or PastDue subscription may have its package/version, billing cycle, or AutoRenew
/// changed; Restricted/Canceled/Expired subscriptions are terminal for this purpose (a lapsed
/// subscription is resubscribed via provisioning, not changed in place).</summary>
public sealed class OrganizationSubscriptionChangeNotAllowedException(OrganizationSubscriptionId id, OrganizationSubscriptionStatus status)
    : DomainException($"Organization subscription {id} cannot be changed while in status {status}.");
