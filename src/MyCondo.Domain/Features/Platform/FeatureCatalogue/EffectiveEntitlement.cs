namespace MyCondo.Domain.Features.Platform.FeatureCatalogue;

/// <summary>
/// One feature's resolved entitlement for a tenant (ADR-033 §13/§19/§20) — the per-feature output of
/// <see cref="TenantEntitlementResolution.Resolve"/>. <see cref="LimitValue"/> is only ever non-null when
/// <see cref="EntitlementType"/> is <see cref="FeatureEntitlementType.Numeric"/>, the feature is
/// <see cref="Enabled"/>, and the winning grant (currently only a package row — a
/// <see cref="TenantFeatureOverrides.TenantFeatureOverride"/> has no limit field to carry, ADR-033 §18)
/// specified one.
/// </summary>
public sealed record EffectiveEntitlement(
    string FeatureKey,
    bool Enabled,
    FeatureEntitlementType EntitlementType,
    int? LimitValue,
    EntitlementSource Source);
