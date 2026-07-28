using MyCondo.Application.Features.Auth.DTOs;

namespace MyCondo.Application.Common.Abstractions;

public interface ITokenService
{
    /// <summary>
    /// Issues a fresh access + refresh token pair for the given authenticated user.
    /// Persists the refresh token (hashed) before returning.
    /// </summary>
    Task<AuthTokensDto> IssueAsync(
        AuthenticatedUserDto user,
        string ipAddress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates a refresh token (looks up by hash, checks not expired/revoked, and — as
    /// defense-in-depth alongside RLS — that it belongs to <paramref name="tenantId"/>), revokes the
    /// old one, issues a fresh access + refresh pair. Returns null if validation fails.
    /// </summary>
    Task<AuthTokensDto?> RotateAsync(
        Guid tenantId,
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken);

    /// <summary>Revokes the refresh token (logout). No-op if already revoked.</summary>
    Task RevokeAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken);
}
