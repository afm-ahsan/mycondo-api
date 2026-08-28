namespace MyCondo.Domain.Features.Platform.FeatureCatalogue;

/// <summary>
/// Explains which step of <see cref="TenantEntitlementResolution"/>'s precedence (ADR-033 §12/§13)
/// produced an <see cref="EffectiveEntitlement"/> — supportability/shadow-verification metadata only
/// (ADR-033 §20), never itself a source of truth read back into resolution.
/// </summary>
public enum EntitlementSource
{
    Core,
    Reserved,
    TenantOverrideEnabled,
    TenantOverrideDisabled,
    Package,
    DefaultDisabled
}
