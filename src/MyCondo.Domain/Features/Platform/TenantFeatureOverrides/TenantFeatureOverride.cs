using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;

namespace MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

/// <summary>
/// A tenant-specific commercial/product entitlement exception (ADR-033 §12) — pilot, promotional,
/// negotiated enterprise capability, grandfathered feature, or temporary support exception. Not a user
/// permission. Lives in the <c>platform</c> schema, no FK to <c>tenancy.tenants</c> — same no-FK
/// convention as <c>TenantModule</c>/<c>PlatformAuditLogEntry.TenantId</c>.
///
/// <para>Wins over the tenant's package outright, in either direction, while its
/// <c>[EffectiveFrom, EffectiveUntil)</c> window contains "now" — except for a core feature, which
/// this type refuses to target at all (see <see cref="TenantFeatureOverrideEligibility"/>), since the
/// resolver never consults an override for one anyway (ADR-033 §6/§12 precedence).</para>
///
/// <para>At most one override may be simultaneously effective for a given (TenantId, FeatureId) pair —
/// enforced by a Postgres <c>EXCLUDE USING gist</c> constraint (see
/// <c>TenantFeatureOverrideConfiguration</c>/the owning migration), the same mechanism already used for
/// <c>ServiceChargeRule</c>/<c>RatePlan</c>/<c>Booking</c>'s own overlap guards, so the resolver's "look
/// up <em>a</em> TenantFeatureOverride whose window contains now" (ADR-033 §13 step 4) is never
/// ambiguous.</para>
/// </summary>
public sealed class TenantFeatureOverride : Entity<TenantFeatureOverrideId>
{
    public Guid TenantId { get; private set; }
    public FeatureDefinitionId FeatureId { get; private set; }
    public bool Enabled { get; private set; }

    public DateTimeOffset EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveUntil { get; private set; }

    public string? Reason { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TenantFeatureOverride() { }

    private TenantFeatureOverride(
        TenantFeatureOverrideId id,
        Guid tenantId,
        FeatureDefinitionId featureId,
        bool enabled,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil,
        string? reason,
        Guid createdBy,
        DateTimeOffset createdAtUtc) : base(id)
    {
        TenantId = tenantId;
        FeatureId = featureId;
        Enabled = enabled;
        EffectiveFrom = effectiveFrom;
        EffectiveUntil = effectiveUntil;
        Reason = reason;
        CreatedBy = createdBy;
        CreatedAt = createdAtUtc;
    }

    /// <summary>
    /// <paramref name="feature"/> is the already-loaded <see cref="FeatureDefinition"/> this override
    /// targets — required so <see cref="TenantFeatureOverrideEligibility"/> can reject a core or
    /// Reserved feature before the row is ever constructed.
    /// </summary>
    public static TenantFeatureOverride Create(
        Guid tenantId,
        FeatureDefinition feature,
        bool enabled,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil,
        string? reason,
        Guid createdBy,
        DateTimeOffset createdAtUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(feature);

        TenantFeatureOverrideEligibility.Validate(feature);

        if (effectiveUntil is not null && effectiveUntil < effectiveFrom)
        {
            throw new ArgumentException("EffectiveUntil must not be before EffectiveFrom.", nameof(effectiveUntil));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("CreatedBy is required.", nameof(createdBy));
        }

        string? trimmedReason = reason?.Trim();
        if (trimmedReason is { Length: 0 })
        {
            throw new ArgumentException("Reason cannot be whitespace-only.", nameof(reason));
        }

        return new TenantFeatureOverride(
            TenantFeatureOverrideId.New(), tenantId, feature.Id, enabled, effectiveFrom, effectiveUntil,
            trimmedReason, createdBy, createdAtUtc);
    }
}
