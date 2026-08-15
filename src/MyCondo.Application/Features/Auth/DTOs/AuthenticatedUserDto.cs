namespace MyCondo.Application.Features.Auth.DTOs;

/// <summary>A permission granted to the user, scoped to exactly one building.</summary>
public sealed record BuildingPermissionGrant(Guid BuildingId, string Permission);

/// <summary>
/// <paramref name="Permissions"/> holds only tenant-wide grants (from role assignments with no
/// building scope) — this is exactly what the JWT's `perm` claim carries, so the two stay in sync.
/// Building-scoped grants live separately in <paramref name="BuildingPermissions"/> (the JWT's
/// `bperm` claim) precisely so a permission held only for Building A can't be mistaken for a
/// tenant-wide grant that would also apply to Building B. See ADR-014.
/// </summary>
public sealed record AuthenticatedUserDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<Guid> BuildingIds,
    IReadOnlyList<BuildingPermissionGrant> BuildingPermissions,
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
    string Email,
    string FullName,
    string? PhoneNumber,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string? AvatarUrl = null);
