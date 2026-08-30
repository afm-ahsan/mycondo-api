namespace MyCondo.Application.Features.Platform.DTOs;

/// <summary>One raw <see cref="MyCondo.Domain.Features.Platform.TenantFeatureOverrides.TenantFeatureOverride"/>
/// row for platform-admin inspection (ADR-033 Task 13C) — the configured override itself, not the
/// resolved effective value <see cref="EffectiveFeatureEntitlementDto"/> already exposes.</summary>
public sealed record TenantFeatureOverrideDto(
    Guid Id,
    string FeatureKey,
    string FeatureName,
    bool Enabled,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveUntil,
    string? Reason,
    Guid CreatedBy,
    DateTimeOffset CreatedAt);
