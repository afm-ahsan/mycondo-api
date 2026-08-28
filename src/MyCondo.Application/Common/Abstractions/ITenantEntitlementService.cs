namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// The effective tenant entitlement resolver (ADR-033 §13) — answers "is this feature purchased for this
/// organization," never "may the subscription currently use it" (that second, coarser question is
/// <c>SubscriptionLifecycleAllowsAccess</c>, ADR-033 §14, deliberately out of this service's scope) and
/// never "may this user perform this action" (RBAC, unrelated axis, ADR-033 §4).
///
/// <para><b>Read/shadow mode only for now</b> (Task 05) — nothing in the Mediator pipeline or endpoint
/// authorization calls this service yet. Wiring it into request handling (<c>IRequiresFeature</c>,
/// <c>FeatureEntitlementBehavior</c>, ADR-033 §16) is Task 06.</para>
///
/// <para>Tenant identity is always an explicit <c>tenantId</c> parameter — never inferred from
/// email/username/package/feature/request payload (Task 05 §5), so cross-tenant entitlement lookup cannot
/// occur accidentally.</para>
/// </summary>
public interface ITenantEntitlementService
{
    /// <summary>Throws <c>FeatureNotFoundException</c> for an unknown <paramref name="featureKey"/> —
    /// feature keys are developer-authored constants, never user input (ADR-033 §13 step 1).</summary>
    Task<bool> IsFeatureEnabled(Guid tenantId, string featureKey, CancellationToken cancellationToken);

    /// <summary>The tenant's complete effective entitlement set, one dictionary entry per
    /// <c>FeatureDefinition</c> in the catalogue, computed by the same central algorithm as
    /// <see cref="IsFeatureEnabled"/> (ADR-033 §22).</summary>
    Task<IReadOnlyDictionary<string, bool>> GetEffectiveEntitlements(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Numeric-entitlement (<c>FeatureEntitlementType.Numeric</c>) limit for
    /// <paramref name="featureKey"/> — <c>null</c> when the feature is not entitled, is a Boolean feature,
    /// or is entitled with no limit configured (ADR-033 §13/§17/§22). No quota enforcement is performed by
    /// this service or any caller of it.</summary>
    Task<int?> GetEntitlementValue(Guid tenantId, string featureKey, CancellationToken cancellationToken);
}
