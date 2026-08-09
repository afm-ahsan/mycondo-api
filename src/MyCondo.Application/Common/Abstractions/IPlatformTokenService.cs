using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Mirrors <see cref="ITokenService"/>'s contract and security practices (hashed refresh-token
/// storage, rotation, revocation) but issues/consumes Platform-scope tokens against the separate
/// <c>platform_refresh_tokens</c> store — never the tenant <c>RefreshToken</c> table.
/// </summary>
public interface IPlatformTokenService
{
    Task<PlatformAuthTokensDto> IssueAsync(
        PlatformAuthenticatedUserDto user,
        string ipAddress,
        CancellationToken cancellationToken);

    /// <summary>Returns null if the refresh token is unknown, expired, revoked, or the platform user
    /// is no longer active.</summary>
    Task<PlatformAuthTokensDto?> RotateAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken);

    Task RevokeAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken);
}
