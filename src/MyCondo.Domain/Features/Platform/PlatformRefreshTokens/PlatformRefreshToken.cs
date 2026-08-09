using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Domain.Features.Platform.PlatformRefreshTokens;

/// <summary>
/// Mirrors <see cref="MyCondo.Domain.Features.Identity.RefreshTokens.RefreshToken"/>'s shape and
/// security practices (hashed storage, rotation, revocation) but is a physically separate table keyed
/// on <see cref="PlatformUserId"/> — never reuses tenant refresh-token rows with a nullable TenantId.
/// </summary>
public sealed class PlatformRefreshToken : Entity<PlatformRefreshTokenId>
{
    public PlatformUserId PlatformUserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string CreatedByIp { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }
    public PlatformRefreshTokenId? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresAtUtc;
    public bool IsActive(DateTimeOffset nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    private PlatformRefreshToken()
    {
        TokenHash = null!;
        CreatedByIp = null!;
    }

    private PlatformRefreshToken(
        PlatformRefreshTokenId id,
        PlatformUserId platformUserId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc,
        string createdByIp) : base(id)
    {
        PlatformUserId = platformUserId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = nowUtc;
        CreatedByIp = createdByIp;
    }

    public static PlatformRefreshToken Issue(
        PlatformUserId platformUserId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc,
        string createdByIp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByIp);
        return new PlatformRefreshToken(
            PlatformRefreshTokenId.New(), platformUserId, tokenHash, expiresAtUtc, nowUtc, createdByIp);
    }

    public void Revoke(DateTimeOffset nowUtc, string revokedByIp, PlatformRefreshTokenId? replacedByTokenId = null)
    {
        if (IsRevoked)
        {
            return;
        }
        RevokedAtUtc = nowUtc;
        RevokedByIp = revokedByIp;
        ReplacedByTokenId = replacedByTokenId;
    }
}
