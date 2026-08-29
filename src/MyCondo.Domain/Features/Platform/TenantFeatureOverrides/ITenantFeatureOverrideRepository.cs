using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

public interface ITenantFeatureOverrideRepository
{
    Task<List<TenantFeatureOverride>> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Tracked lookup by id (Task 13C) — for Update/End command handlers, unlike
    /// <see cref="GetForTenantAsync"/>'s no-tracking read-model use.</summary>
    Task<TenantFeatureOverride?> GetByIdAsync(TenantFeatureOverrideId id, CancellationToken cancellationToken);

    /// <summary>Application-layer pre-check ahead of the authoritative DB EXCLUDE constraint — gives a
    /// friendly 409 instead of a raw constraint-violation error for the common case (mirrors
    /// IServiceChargeRuleRepository.HasOverlappingRuleAsync).</summary>
    /// <param name="tenantId">The tenant the candidate override targets.</param>
    /// <param name="featureId">The feature the candidate override targets.</param>
    /// <param name="effectiveFrom">The candidate override's proposed window start.</param>
    /// <param name="effectiveUntil">The candidate override's proposed window end, or null if open-ended.</param>
    /// <param name="excludeId">When updating an existing override's own window (Task 13C), its own id —
    /// excluded so it is never reported as overlapping itself.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> HasOverlappingOverrideAsync(
        Guid tenantId,
        FeatureDefinitionId featureId,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil,
        TenantFeatureOverrideId? excludeId,
        CancellationToken cancellationToken);

    void Add(TenantFeatureOverride tenantFeatureOverride);
}
