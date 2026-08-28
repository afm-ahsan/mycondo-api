using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

public interface ITenantFeatureOverrideRepository
{
    Task<List<TenantFeatureOverride>> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Application-layer pre-check ahead of the authoritative DB EXCLUDE constraint — gives a
    /// friendly 409 instead of a raw constraint-violation error for the common case (mirrors
    /// IServiceChargeRuleRepository.HasOverlappingRuleAsync).</summary>
    Task<bool> HasOverlappingOverrideAsync(
        Guid tenantId,
        FeatureDefinitionId featureId,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil,
        CancellationToken cancellationToken);

    void Add(TenantFeatureOverride tenantFeatureOverride);
}
