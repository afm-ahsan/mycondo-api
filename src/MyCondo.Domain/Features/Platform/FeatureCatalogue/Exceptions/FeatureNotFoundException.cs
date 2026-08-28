using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Platform.FeatureCatalogue.Exceptions;

/// <summary>A resolver lookup referenced a Feature Catalogue key with no <see cref="FeatureDefinition"/>
/// row (ADR-033 §13 step 1). Feature keys are developer-authored constants passed by
/// <c>IRequiresFeature</c>-style callers, never user input — an unknown key is a code defect, not a
/// runtime condition to degrade gracefully from.</summary>
public sealed class FeatureNotFoundException(string featureKey)
    : DomainException($"No FeatureDefinition exists for key '{featureKey}'.");
