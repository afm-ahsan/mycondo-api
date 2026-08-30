using MyCondo.Domain.Exceptions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

/// <summary>ADR-033 §9/§11 — a package version's feature composition must assign each
/// <see cref="FeatureDefinitionId"/> at most once.</summary>
public sealed class DuplicatePackageFeatureAssignmentException(
    SubscriptionPackageVersionId packageVersionId, FeatureDefinitionId featureId)
    : DomainException(
        $"Feature {featureId} is assigned more than once to subscription package version {packageVersionId}.");
