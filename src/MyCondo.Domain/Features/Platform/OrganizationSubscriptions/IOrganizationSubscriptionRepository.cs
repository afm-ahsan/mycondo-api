namespace MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

public interface IOrganizationSubscriptionRepository
{
    Task<OrganizationSubscription?> GetByIdAsync(OrganizationSubscriptionId id, CancellationToken cancellationToken);

    /// <summary>The tenant's current (non-terminal: Active/PastDue/Restricted) subscription, if any —
    /// see the partial unique index enforcing at most one such row per tenant.</summary>
    Task<OrganizationSubscription?> GetCurrentForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    void Add(OrganizationSubscription subscription);
}
