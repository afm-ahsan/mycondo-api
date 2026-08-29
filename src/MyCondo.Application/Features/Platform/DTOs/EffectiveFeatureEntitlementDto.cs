namespace MyCondo.Application.Features.Platform.DTOs;

/// <summary>One feature's resolved entitlement for an organization, for platform-admin inspection
/// (ADR-033 Task 13A). <see cref="Source"/> exposes which step of the entitlement precedence produced
/// <see cref="Enabled"/> (Core/Reserved short-circuit, tenant override, package grant, or default) —
/// unlike the tenant-facing session contract, this platform read surface is not required to redact it.</summary>
public sealed record EffectiveFeatureEntitlementDto(
    string FeatureKey,
    bool Enabled,
    string EntitlementType,
    int? LimitValue,
    string Source);
