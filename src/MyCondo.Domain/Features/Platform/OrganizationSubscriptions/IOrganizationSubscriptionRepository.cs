namespace MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

public interface IOrganizationSubscriptionRepository
{
    Task<OrganizationSubscription?> GetByIdAsync(OrganizationSubscriptionId id, CancellationToken cancellationToken);

    /// <summary>The tenant's current (non-terminal: Active/PastDue/Restricted) subscription, if any —
    /// see the partial unique index enforcing at most one such row per tenant.</summary>
    Task<OrganizationSubscription?> GetCurrentForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>The tenant's most recently activated subscription row regardless of status — including
    /// terminal <see cref="OrganizationSubscriptionStatus.Expired"/>/<see cref="OrganizationSubscriptionStatus.Canceled"/>
    /// rows that <see cref="GetCurrentForTenantAsync"/> deliberately excludes. Used by subscription
    /// lifecycle access evaluation (ADR-032 §4/§21) to distinguish "subscription lapsed" from "no
    /// subscription has ever been provisioned for this tenant" — <c>null</c> only means the latter.</summary>
    Task<OrganizationSubscription?> GetLatestForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    void Add(OrganizationSubscription subscription);
}
