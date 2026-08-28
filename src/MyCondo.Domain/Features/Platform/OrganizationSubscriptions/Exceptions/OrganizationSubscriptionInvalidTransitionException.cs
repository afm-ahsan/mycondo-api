using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;

/// <summary>Guards <see cref="OrganizationSubscription"/>'s lifecycle state machine (ADR-032 §4) —
/// thrown for any transition attempt other than Active⇄PastDue, PastDue→Restricted,
/// Restricted/Canceled→Expired, or Active/PastDue→Canceled.</summary>
public sealed class OrganizationSubscriptionInvalidTransitionException(
    OrganizationSubscriptionId id, OrganizationSubscriptionStatus from, OrganizationSubscriptionStatus to)
    : DomainException($"Organization subscription {id} cannot transition from {from} to {to}.");
