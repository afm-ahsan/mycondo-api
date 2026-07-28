using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Settings;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.RefreshTokens;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Infrastructure.Identity;

public sealed class JwtTokenService(
    IOptions<JwtSettings> jwtOptions,
    IUserContextResolver userContextResolver,
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<JwtTokenService> logger
) : ITokenService
{
    private readonly JwtSettings _settings = jwtOptions.Value;

    public async Task<AuthTokensDto> IssueAsync(
        AuthenticatedUserDto user,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        DateTimeOffset nowUtc = clock.UtcNow;
        DateTimeOffset accessExpires = nowUtc.AddMinutes(_settings.AccessTokenMinutes);
        DateTimeOffset refreshExpires = nowUtc.AddDays(_settings.RefreshTokenDays);

        string accessToken = WriteAccessToken(user, accessExpires);

        string rawRefresh = GenerateRefreshTokenString();
        string refreshHash = HashRefreshToken(rawRefresh);

        RefreshToken refreshEntity = RefreshToken.Issue(
            tenantId: user.TenantId,
            userId: new UserId(user.UserId),
            tokenHash: refreshHash,
            expiresAtUtc: refreshExpires,
            nowUtc: nowUtc,
            createdByIp: ipAddress);

        refreshTokens.Add(refreshEntity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(
            AccessToken: accessToken,
            AccessTokenExpiresAtUtc: accessExpires,
            RefreshToken: rawRefresh,
            RefreshTokenExpiresAtUtc: refreshExpires,
            User: user);
    }

    public async Task<AuthTokensDto?> RotateAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        string hash = HashRefreshToken(refreshToken);
        RefreshToken? existing = await refreshTokens.GetByTokenHashAsync(hash, cancellationToken);

        if (existing is null)
        {
            logger.LogInformation("Refresh-token rotation: token not found");
            return null;
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        if (!existing.IsActive(nowUtc))
        {
            logger.LogInformation(
                "Refresh-token rotation: token {TokenId} inactive (revoked={IsRevoked}, expired={IsExpired})",
                existing.Id, existing.IsRevoked, existing.IsExpired(nowUtc));
            return null;
        }

        User? user = await users.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            return null;
        }

        // Revoke old refresh token; new one is issued by IssueAsync below.
        existing.Revoke(nowUtc, ipAddress);

        AuthenticatedUserDto auth = await userContextResolver.ResolveAsync(user, cancellationToken);
        AuthTokensDto fresh = await IssueAsync(auth, ipAddress, cancellationToken);
        return fresh;
    }

    public async Task RevokeAsync(string refreshToken, string ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        string hash = HashRefreshToken(refreshToken);
        RefreshToken? existing = await refreshTokens.GetByTokenHashAsync(hash, cancellationToken);

        if (existing is null || existing.IsRevoked)
        {
            return;
        }

        existing.Revoke(clock.UtcNow, ipAddress);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private string WriteAccessToken(AuthenticatedUserDto user, DateTimeOffset expiresAt)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_settings.SigningKey));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new("tenant_id", user.TenantId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
        ];

        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(user.Permissions.Select(p => new Claim("perm", p)));
        claims.AddRange(user.BuildingIds.Select(b => new Claim("building_ids", b.ToString())));

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
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
        // SHA-256 of the raw token: stored in the DB; rotation/revocation lookups are by hash.
        // Refresh tokens are random 64-byte secrets (not user-derived) so a fast hash is sufficient.
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(token), hash);
        return Convert.ToHexString(hash);
    }
}
