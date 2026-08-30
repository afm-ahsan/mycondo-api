using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.FeatureCatalogue.Exceptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Application.Common.Services;

/// <summary>
/// Production implementation of <see cref="ITenantEntitlementService"/> (ADR-033 §13, Task 05) — read-only,
/// no caching yet (ADR-033 §36), never reads legacy <c>TenantModule</c> (ADR-033 §2/Task 05 §24). Delegates
/// the actual precedence decision to <see cref="TenantEntitlementResolution.Resolve"/> so this class and
/// <see cref="LegacyEntitlementShadowComparer"/>'s simulation share one algorithm (ADR-033 §22/§29).
///
/// <para><b>Query strategy (ADR-033 §21/§43):</b> exactly four queries regardless of catalogue size or
/// which/how-many features are requested — the full <c>FeatureDefinition</c> catalogue, the tenant's
/// current <c>OrganizationSubscription</c>, that subscription's package-version feature composition (empty
/// when no subscription), and the tenant's own <c>TenantFeatureOverride</c> rows — then resolves every
/// feature in memory. No query per feature, no query per caller.</para>
/// </summary>
public sealed class TenantEntitlementService(
    IFeatureDefinitionRepository featureDefinitions,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    ISubscriptionPackageFeatureRepository subscriptionPackageFeatures,
    ITenantFeatureOverrideRepository tenantFeatureOverrides,
    IClock clock
) : ITenantEntitlementService
{
    public async Task<bool> IsFeatureEnabled(Guid tenantId, string featureKey, CancellationToken cancellationToken)
    {
        Dictionary<string, EffectiveEntitlement> effective = await ResolveAllAsync(tenantId, cancellationToken);

        if (!effective.TryGetValue(featureKey, out EffectiveEntitlement? entitlement))
        {
            throw new FeatureNotFoundException(featureKey);
        }

        return entitlement.Enabled;
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetEffectiveEntitlements(Guid tenantId, CancellationToken cancellationToken)
    {
        Dictionary<string, EffectiveEntitlement> effective = await ResolveAllAsync(tenantId, cancellationToken);
        return effective.ToDictionary(kv => kv.Key, kv => kv.Value.Enabled, StringComparer.Ordinal);
    }

    public async Task<int?> GetEntitlementValue(Guid tenantId, string featureKey, CancellationToken cancellationToken)
    {
        Dictionary<string, EffectiveEntitlement> effective = await ResolveAllAsync(tenantId, cancellationToken);

        if (!effective.TryGetValue(featureKey, out EffectiveEntitlement? entitlement))
        {
            throw new FeatureNotFoundException(featureKey);
        }

        return entitlement.LimitValue;
    }

    public async Task<IReadOnlyList<EffectiveEntitlement>> GetEffectiveEntitlementDetails(Guid tenantId, CancellationToken cancellationToken)
    {
        Dictionary<string, EffectiveEntitlement> effective = await ResolveAllAsync(tenantId, cancellationToken);
        return effective.Values.ToList();
    }

    private async Task<Dictionary<string, EffectiveEntitlement>> ResolveAllAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        List<FeatureDefinition> catalogue = await featureDefinitions.GetAllAsync(cancellationToken);
        OrganizationSubscription? subscription =
            await organizationSubscriptions.GetCurrentForTenantAsync(tenantId, cancellationToken);

        List<SubscriptionPackageFeature> assignedFeatures = subscription is null
            ? []
            : await subscriptionPackageFeatures.GetForPackageVersionAsync(subscription.PackageVersionId, cancellationToken);

        List<TenantFeatureOverride> overrides = await tenantFeatureOverrides.GetForTenantAsync(tenantId, cancellationToken);

        DateTimeOffset nowUtc = clock.UtcNow;

        // At most one override may be simultaneously effective per (TenantId, FeatureId) — enforced by a
        // Postgres EXCLUDE constraint (TenantFeatureOverride's own doc comment). ToDictionary throws if
        // that invariant is somehow violated, which is the correct "fail safely rather than silently
        // choosing one" behavior Task 05 §10 requires, not a case to swallow.
        Dictionary<FeatureDefinitionId, TenantFeatureOverride> effectiveOverrideByFeature = overrides
            .Where(o => o.EffectiveFrom <= nowUtc && (o.EffectiveUntil is null || nowUtc < o.EffectiveUntil.Value))
            .ToDictionary(o => o.FeatureId);

        Dictionary<FeatureDefinitionId, SubscriptionPackageFeature> packageFeatureByFeature = assignedFeatures
            .ToDictionary(f => f.FeatureId);

        Dictionary<string, EffectiveEntitlement> result = new(StringComparer.Ordinal);
        foreach (FeatureDefinition feature in catalogue)
        {
            effectiveOverrideByFeature.TryGetValue(feature.Id, out TenantFeatureOverride? effectiveOverride);
            packageFeatureByFeature.TryGetValue(feature.Id, out SubscriptionPackageFeature? packageFeature);

            result[feature.Key] = TenantEntitlementResolution.Resolve(
                feature, subscription is not null, effectiveOverride, packageFeature);
        }

        return result;
    }
}
