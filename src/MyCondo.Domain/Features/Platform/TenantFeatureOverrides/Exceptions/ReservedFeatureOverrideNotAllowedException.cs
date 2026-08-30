using MyCondo.Domain.Exceptions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.Features.Platform.TenantFeatureOverrides.Exceptions;

/// <summary>A <see cref="FeatureDefinition"/> with <see cref="FeatureCatalogueStatus.Reserved"/> status
/// is a legacy migration-bridge placeholder — "representable, never enabled in any package or UI"
/// (ADR-033 §24 step 2). A tenant feature override would make it commercially assignable outside that
/// migration path, which ADR-033 does not authorize.</summary>
public sealed class ReservedFeatureOverrideNotAllowedException(FeatureDefinitionId featureId)
    : DomainException($"Feature {featureId} is Reserved and cannot be targeted by a tenant feature override.");
