using MyCondo.Domain.Exceptions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

/// <summary>ADR-033 §9 — enabling a leaf feature requires its full parent chain to also carry an
/// <c>Enabled = true</c> row in the same package version; parent/child inheritance is never inferred at
/// resolution time. Enabling a parent does not implicitly enable children (no exception needed for that
/// direction — it's simply a no-op).</summary>
public sealed class MissingParentFeatureAssignmentException(
    SubscriptionPackageVersionId packageVersionId, FeatureDefinitionId featureId, FeatureDefinitionId parentFeatureId)
    : DomainException(
        $"Subscription package version {packageVersionId} enables feature {featureId} without its required " +
        $"parent {parentFeatureId} also being enabled in the same version.");
