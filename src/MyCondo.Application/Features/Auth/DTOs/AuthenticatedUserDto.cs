namespace MyCondo.Application.Features.Auth.DTOs;

public sealed record AuthenticatedUserDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<Guid> BuildingIds);

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
    IReadOnlyList<string> Permissions);
