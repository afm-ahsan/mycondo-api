using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Settings;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.PlatformRefreshTokens;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Infrastructure.Identity;

/// <summary>
/// Platform-scope analogue of <see cref="JwtTokenService"/>. Kept as a physically separate class
/// (not a branch inside JwtTokenService) so a Platform token can never accidentally end up shaped
/// like a tenant token or vice versa — see mycondo-docs ADR-019.
///
/// Two security-relevant differences from the tenant token: (1) `aud` is
/// <see cref="JwtSettings.PlatformAudience"/>, never <see cref="JwtSettings.Audience"/>; (2) there is
/// no `tenant_id` claim at all — not null, not empty string, structurally absent, because
/// <see cref="PlatformAuthenticatedUserDto"/> has no TenantId field to read one from.
/// </summary>
public sealed class PlatformJwtTokenService(
    IOptions<JwtSettings> jwtOptions,
    IPlatformUserContextResolver contextResolver,
    IPlatformRefreshTokenRepository refreshTokens,
    IPlatformUserRepository platformUsers,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<PlatformJwtTokenService> logger
) : IPlatformTokenService
{
    private readonly JwtSettings _settings = jwtOptions.Value;

    public async Task<PlatformAuthTokensDto> IssueAsync(
        PlatformAuthenticatedUserDto user,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        DateTimeOffset nowUtc = clock.UtcNow;
        DateTimeOffset accessExpires = nowUtc.AddMinutes(_settings.AccessTokenMinutes);
        DateTimeOffset refreshExpires = nowUtc.AddDays(_settings.RefreshTokenDays);

        string accessToken = WriteAccessToken(user, accessExpires);

        string rawRefresh = GenerateRefreshTokenString();
        string refreshHash = HashRefreshToken(rawRefresh);

        PlatformRefreshToken refreshEntity = PlatformRefreshToken.Issue(
            platformUserId: new PlatformUserId(user.PlatformUserId),
            tokenHash: refreshHash,
            expiresAtUtc: refreshExpires,
            nowUtc: nowUtc,
            createdByIp: ipAddress);

        refreshTokens.Add(refreshEntity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlatformAuthTokensDto(
            AccessToken: accessToken,
            AccessTokenExpiresAtUtc: accessExpires,
            RefreshToken: rawRefresh,
            RefreshTokenExpiresAtUtc: refreshExpires,
            User: user);
    }

    public async Task<PlatformAuthTokensDto?> RotateAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        string hash = HashRefreshToken(refreshToken);
        PlatformRefreshToken? existing = await refreshTokens.GetByTokenHashAsync(hash, cancellationToken);

        if (existing is null)
        {
            logger.LogInformation("Platform refresh-token rotation: token not found");
            return null;
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        if (!existing.IsActive(nowUtc))
        {
            logger.LogInformation(
                "Platform refresh-token rotation: token {TokenId} inactive (revoked={IsRevoked}, expired={IsExpired})",
                existing.Id, existing.IsRevoked, existing.IsExpired(nowUtc));
            return null;
        }

        PlatformUser? user = await platformUsers.GetByIdAsync(existing.PlatformUserId, cancellationToken);
        if (user is null || user.Status != PlatformUserStatus.Active)
        {
            return null;
        }

        existing.Revoke(nowUtc, ipAddress);

        PlatformAuthenticatedUserDto auth = await contextResolver.ResolveAsync(user, cancellationToken);
        PlatformAuthTokensDto fresh = await IssueAsync(auth, ipAddress, cancellationToken);
        return fresh;
    }

    public async Task RevokeAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        string hash = HashRefreshToken(refreshToken);
        PlatformRefreshToken? existing = await refreshTokens.GetByTokenHashAsync(hash, cancellationToken);

        if (existing is null || existing.IsRevoked)
        {
            return;
        }

        existing.Revoke(clock.UtcNow, ipAddress);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private string WriteAccessToken(PlatformAuthenticatedUserDto user, DateTimeOffset expiresAt)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_settings.SigningKey));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.PlatformUserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new("identity_scope", "platform"),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
            // Deliberately no "tenant_id" claim — see this class's doc comment.
        ];

        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(user.Permissions.Select(p => new Claim("perm", p)));

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = _settings.Issuer,
            Audience = _settings.PlatformAudience,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = creds
        };

        JsonWebTokenHandler handler = new();
        return handler.CreateToken(descriptor);
    }

    private static string GenerateRefreshTokenString()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashRefreshToken(string token)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(token), hash);
        return Convert.ToHexString(hash);
    }
}
