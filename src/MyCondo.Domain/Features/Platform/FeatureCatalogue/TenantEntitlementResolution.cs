using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Domain.Features.Platform.FeatureCatalogue;

/// <summary>
/// The single authoritative per-feature resolution algorithm behind <c>ITenantEntitlementService</c>
/// (ADR-033 §13) — a pure, static, no-I/O function over already-loaded data, mirroring
/// <c>SubscriptionPackageFeatureComposition.Validate</c>'s and
/// <c>OrganizationSubscriptionCommercialTerms.ResolveBasePrice</c>'s own shape. Reused by both the
/// production resolver (<c>TenantEntitlementService</c>, one DB round-trip per input, no I/O here) and
/// <c>LegacyEntitlementShadowComparer</c>'s pre-migration simulation, so there is exactly one
/// implementation of the precedence rule to keep correct (ADR-033 §22/§29).
///
/// <para><b>Exact precedence (ADR-033 §12/§13):</b></para>
/// <list type="number">
/// <item><see cref="FeatureDefinition.IsCore"/> → always enabled, nothing else consulted.</item>
/// <item><see cref="FeatureCatalogueStatus.Reserved"/> → always disabled, nothing else consulted. Not one
/// of the ADR's literal numbered steps — added defensively per the Task 05 brief's explicit "must not
/// accidentally become enabled" requirement, since nothing at the persistence layer stops a Reserved
/// feature from acquiring an <c>Enabled = true</c> <see cref="SubscriptionPackageFeature"/> row (a
/// <see cref="TenantFeatureOverride"/> is already blocked from ever targeting one, ADR-033 Task 03).</item>
/// <item>No current subscription → disabled. Checked before overrides, exactly ADR-033 §13 step 3's
/// literal ordering — an override is never consulted when there is no subscription to override.</item>
/// <item>An effective <see cref="TenantFeatureOverride"/> → its <c>Enabled</c> value, final.</item>
/// <item>A <see cref="SubscriptionPackageFeature"/> row for the subscription's package version → its
/// <c>Enabled</c> value (and <c>LimitValue</c> when enabled).</item>
/// <item>Otherwise → disabled (default).</item>
/// </list>
/// </summary>
public static class TenantEntitlementResolution
{
    public static EffectiveEntitlement Resolve(
        FeatureDefinition feature,
        bool hasCurrentSubscription,
        TenantFeatureOverride? effectiveOverride,
        SubscriptionPackageFeature? packageFeature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (feature.IsCore)
        {
            return new EffectiveEntitlement(feature.Key, true, feature.EntitlementType, null, EntitlementSource.Core);
        }

        if (feature.Status == FeatureCatalogueStatus.Reserved)
        {
            return new EffectiveEntitlement(feature.Key, false, feature.EntitlementType, null, EntitlementSource.Reserved);
        }

        if (!hasCurrentSubscription)
        {
            return new EffectiveEntitlement(feature.Key, false, feature.EntitlementType, null, EntitlementSource.DefaultDisabled);
        }

        if (effectiveOverride is not null)
        {
            EntitlementSource source = effectiveOverride.Enabled
                ? EntitlementSource.TenantOverrideEnabled
                : EntitlementSource.TenantOverrideDisabled;
            return new EffectiveEntitlement(feature.Key, effectiveOverride.Enabled, feature.EntitlementType, null, source);
        }

        if (packageFeature is not null)
        {
            int? limitValue = packageFeature.Enabled ? packageFeature.LimitValue : null;
            return new EffectiveEntitlement(feature.Key, packageFeature.Enabled, feature.EntitlementType, limitValue, EntitlementSource.Package);
        }

        return new EffectiveEntitlement(feature.Key, false, feature.EntitlementType, null, EntitlementSource.DefaultDisabled);
    }
}
