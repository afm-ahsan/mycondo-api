using MyCondo.Domain.Exceptions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

/// <summary>ADR-033 §11 — every package-feature assignment must reference a real, known
/// <see cref="FeatureDefinition"/>; an unknown feature key is a catalogue-authoring bug, not a runtime
/// condition to degrade gracefully from (same reasoning as the future resolver's
/// <c>FeatureNotFoundException</c>, ADR-033 §13).</summary>
public sealed class UnknownPackageFeatureException(SubscriptionPackageVersionId packageVersionId, FeatureDefinitionId featureId)
    : DomainException(
        $"Subscription package version {packageVersionId} assigns unknown feature {featureId}.");
