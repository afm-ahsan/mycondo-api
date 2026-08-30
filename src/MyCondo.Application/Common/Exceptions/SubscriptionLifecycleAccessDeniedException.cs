using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

namespace MyCondo.Application.Common.Exceptions;

/// <summary>
/// The organization's subscription is in a read-only lifecycle state (Restricted/Expired/Canceled,
/// ADR-032 §5) and this request is an ordinary write — distinct from <see cref="FeatureNotEntitledException"/>
/// (the tenant may still own the feature; the subscription tier itself is what currently blocks writes) so
/// the API can map it to its own <c>subscription_restricted</c>/<c>subscription_expired</c> error codes
/// (ADR-032 Task 10 §16/§27). <see cref="OrganizationSubscriptionStatus.Canceled"/> is reported under the
/// same <c>subscription_expired</c> code as <see cref="OrganizationSubscriptionStatus.Expired"/> — ADR-032
/// §5's access table gives both identical treatment, and Task 10 §27 favors one structured contract over
/// unnecessary code proliferation.
/// </summary>
public sealed class SubscriptionLifecycleAccessDeniedException : ApplicationException
{
    public Guid TenantId { get; }
    public OrganizationSubscriptionStatus SubscriptionStatus { get; }

    public SubscriptionLifecycleAccessDeniedException(Guid tenantId, OrganizationSubscriptionStatus subscriptionStatus)
        : base($"Organization {tenantId}'s subscription is {subscriptionStatus} (read-only); this operation requires write access.")
    {
        TenantId = tenantId;
        SubscriptionStatus = subscriptionStatus;
    }
}
