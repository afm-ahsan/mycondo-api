using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

namespace MyCondo.Application.Common.Services;

/// <summary>
/// Production implementation of <see cref="ISubscriptionLifecycleAccessService"/>. Query strategy (ADR-032
/// Task 10 §45/§46/§47): exactly one query in the common case
/// (<see cref="IOrganizationSubscriptionRepository.GetCurrentForTenantAsync"/> covers Active/PastDue/
/// Restricted, the vast majority of requests); a second query only when no current subscription exists,
/// to distinguish a terminal Expired/Canceled row from "no subscription was ever provisioned" — see
/// <see cref="SubscriptionLifecyclePolicy"/> for why the latter resolves to <see cref="TenantAccessMode.Full"/>.
/// </summary>
public sealed class SubscriptionLifecycleAccessService(
    IOrganizationSubscriptionRepository subscriptions
) : ISubscriptionLifecycleAccessService
{
    public async Task<SubscriptionLifecycleAccess> GetAccessAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        OrganizationSubscription? current = await subscriptions.GetCurrentForTenantAsync(tenantId, cancellationToken);
        if (current is not null)
        {
            return SubscriptionLifecyclePolicy.Evaluate(current.Status);
        }

        OrganizationSubscription? latest = await subscriptions.GetLatestForTenantAsync(tenantId, cancellationToken);
        return SubscriptionLifecyclePolicy.Evaluate(latest?.Status);
    }
}
