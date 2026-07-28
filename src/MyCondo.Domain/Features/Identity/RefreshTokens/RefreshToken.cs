using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Domain.Features.Identity.RefreshTokens;

public sealed class RefreshToken : Entity<RefreshTokenId>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public UserId UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string CreatedByIp { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresAtUtc;
    public bool IsActive(DateTimeOffset nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    private RefreshToken()
    {
        TokenHash = null!;
        CreatedByIp = null!;
    }

    private RefreshToken(
        RefreshTokenId id,
        Guid tenantId,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc,
        string createdByIp) : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = nowUtc;
        CreatedByIp = createdByIp;
    }

    public static RefreshToken Issue(
        Guid tenantId,
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc,
        string createdByIp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByIp);
        return new RefreshToken(
            RefreshTokenId.New(), tenantId, userId, tokenHash, expiresAtUtc, nowUtc, createdByIp);
    }

    public void Revoke(DateTimeOffset nowUtc, string revokedByIp, RefreshTokenId? replacedByTokenId = null)
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
