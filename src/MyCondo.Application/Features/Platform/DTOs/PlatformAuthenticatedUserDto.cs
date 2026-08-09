namespace MyCondo.Application.Features.Platform.DTOs;

/// <summary>
/// Deliberately has no TenantId field — unlike
/// <see cref="MyCondo.Application.Features.Auth.DTOs.AuthenticatedUserDto"/>, there is no tenant
/// dimension to carry. A Platform identity is not a tenant member. See mycondo-docs ADR-019.
/// </summary>
public sealed record PlatformAuthenticatedUserDto(
    Guid PlatformUserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record PlatformAuthTokensDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    PlatformAuthenticatedUserDto User);
