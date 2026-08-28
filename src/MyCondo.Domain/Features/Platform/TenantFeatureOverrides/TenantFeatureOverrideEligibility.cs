using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides.Exceptions;

namespace MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

/// <summary>
/// Creation-time eligibility check for a <see cref="TenantFeatureOverride"/> target feature (ADR-033
/// §12/§25/§26 of Task 03) — a pure, static, no-I/O domain service operating on an already-loaded
/// <see cref="FeatureDefinition"/>, mirroring <c>SubscriptionPackageFeatureComposition</c>'s and
/// <c>OrganizationSubscriptionCommercialTerms</c>'s own shape.
/// </summary>
public static class TenantFeatureOverrideEligibility
{
    public static void Validate(FeatureDefinition feature)
    {
        if (feature.IsCore)
        {
            throw new CoreFeatureOverrideNotAllowedException(feature.Id);
        }

        if (feature.Status != FeatureCatalogueStatus.Active)
        {
            throw new ReservedFeatureOverrideNotAllowedException(feature.Id);
        }
    }
}
