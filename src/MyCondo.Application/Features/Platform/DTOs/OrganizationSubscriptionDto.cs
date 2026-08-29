namespace MyCondo.Application.Features.Platform.DTOs;

/// <summary>Platform read model for one organization's subscription and effective feature state
/// (ADR-033 Task 13A) — organization identity/lifecycle, the latest <c>OrganizationSubscription</c>
/// row regardless of status (<see cref="Subscription"/> is <c>null</c> only when no subscription has
/// ever been provisioned for this tenant, distinct from a lapsed/terminal one), and the same effective
/// entitlement set <c>ITenantEntitlementService</c> computes for the tenant's own session. Carries no
/// tenant operational finance (invoices, payments, ledger).</summary>
public sealed record OrganizationSubscriptionDto(
    Guid TenantId,
    string TenantName,
    string? TenantCode,
    string TenantStatus,
    bool HasSubscription,
    SubscriptionSnapshotDto? Subscription,
    IReadOnlyList<EffectiveFeatureEntitlementDto> Features);
