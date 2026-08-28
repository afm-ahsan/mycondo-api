using MyCondo.Domain.Exceptions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.Features.Platform.TenantFeatureOverrides.Exceptions;

/// <summary>A core feature (<see cref="FeatureDefinition.IsCore"/> = true) is always enabled and never
/// consults an override (ADR-033 §6/§12 precedence step 1) — persisting an override for one would be a
/// permanently dead row the resolver never reads. Prevented at creation time rather than relied upon
/// to be silently ignored later.</summary>
public sealed class CoreFeatureOverrideNotAllowedException(FeatureDefinitionId featureId)
    : DomainException($"Feature {featureId} is a core feature and cannot be targeted by a tenant feature override.");
