namespace MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

/// <summary>
/// The Subscription lifecycle axis (ADR-032 §4), structurally separate from <c>TenantStatus</c>
/// (the Organization lifecycle axis) — deliberately no <c>GracePeriod</c> state (collapsed into
/// <see cref="PastDue"/> for MVP-1.1, ADR-032 §4).
///
/// <para>Transitions: <see cref="Active"/> ⇄ <see cref="PastDue"/> → <see cref="Restricted"/> →
/// <see cref="Expired"/>; <see cref="Active"/>/<see cref="PastDue"/> → <see cref="Canceled"/> →
/// <see cref="Expired"/>. See <see cref="OrganizationSubscription"/>'s transition methods for the
/// exact guarded state machine.</para>
/// </summary>
public enum OrganizationSubscriptionStatus
{
    Active = 0,
    PastDue = 1,
    Restricted = 2,
    Expired = 3,
    Canceled = 4
}
