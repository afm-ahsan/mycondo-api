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
