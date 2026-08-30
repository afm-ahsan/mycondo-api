namespace MyCondo.Domain.Features.Platform.FeatureCatalogue;

/// <summary>
/// "Reserved" marks a <see cref="FeatureDefinition"/> that exists in the catalogue for a
/// <c>TenantModuleKeys</c> legacy key with no current frontend/route surface (ADR-033 §24 step 2) —
/// representable, but never enabled in any <c>SubscriptionPackageFeature</c> or admin console until a
/// roadmap decision promotes it to <see cref="Active"/>.
/// </summary>
public enum FeatureCatalogueStatus
{
    Active = 0,
    Reserved = 1
}
