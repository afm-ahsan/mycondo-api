namespace MyCondo.Application.Features.Auth.DTOs;

/// <summary>A permission granted to the user, scoped to exactly one building.</summary>
public sealed record BuildingPermissionGrant(Guid BuildingId, string Permission);

/// <summary>
/// One catalogue feature's effective entitlement for the tenant (ADR-033 Task 07 §8/§20) — deliberately
/// narrower than the internal <c>EffectiveEntitlement</c> domain record: no <c>Source</c> or
/// <c>EntitlementType</c>, since the frontend never needs to know why a feature is enabled/disabled or
/// distinguish Boolean vs Numeric features, only whether it's enabled and what its limit is (ADR-033
/// Task 07 §9). Includes every catalogue feature, enabled or not (ADR-033 Task 07 §10 Option A) — a
/// Reserved feature always resolves <c>Enabled = false</c>, so it can never appear here as usable.
/// </summary>
public sealed record FeatureEntitlementDto(string FeatureKey, bool Enabled, int? LimitValue);

/// <summary>
/// <paramref name="Permissions"/> holds only tenant-wide grants (from role assignments with no
/// building scope) — this is exactly what the JWT's `perm` claim carries, so the two stay in sync.
/// Building-scoped grants live separately in <paramref name="BuildingPermissions"/> (the JWT's
/// `bperm` claim) precisely so a permission held only for Building A can't be mistaken for a
/// tenant-wide grant that would also apply to Building B. See ADR-014.
///
/// <paramref name="Entitlements"/> is the tenant's effective feature entitlement set (ADR-033 Task 07),
/// resolved once per login/refresh via <c>ITenantEntitlementService</c> — never placed on the JWT itself
/// (ADR-033 Task 07 §4/§16), since entitlements can change without a token refresh and must not go stale
/// for the life of an access token.
///
/// <paramref name="LifecycleAccessMode"/> is one of <c>"Full"</c>, <c>"ReadOnly"</c>, <c>"Denied"</c> —
/// the combined Organization + Subscription lifecycle outcome (ADR-032 Task 10's <c>TenantLifecycleBehavior</c>
/// decision, resolved once per login/refresh so the frontend never has to re-derive the policy itself,
/// ADR-032 Task 11 §5/§7). <paramref name="LifecycleReason"/> is the matching safe reason code
/// (<c>tenant_suspended</c>, <c>tenant_closed</c>, <c>subscription_restricted</c>, <c>subscription_expired</c>)
/// — the same codes <c>GlobalExceptionMiddleware</c> attaches to a lifecycle-denied 403 — or <c>null</c> when
/// the mode is <c>"Full"</c>. Neither field carries package/pricing/billing detail (ADR-032 Task 11 §6).
/// </summary>
public sealed record AuthenticatedUserDto(
    Guid UserId,
    Guid TenantId,
    string TenantName,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<Guid> BuildingIds,
    IReadOnlyList<BuildingPermissionGrant> BuildingPermissions,
    IReadOnlyList<FeatureEntitlementDto> Entitlements,
    string LifecycleAccessMode = "Full",
    string? LifecycleReason = null,
    string? AvatarUrl = null);

public sealed record AuthTokensDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    AuthenticatedUserDto User);

public sealed record UserProfileDto(
    Guid UserId,
    Guid TenantId,
    string TenantName,
    string Email,
    string FullName,
    string? PhoneNumber,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string? AvatarUrl = null);
