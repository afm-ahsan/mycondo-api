using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Domain.Features.Identity.RefreshTokens;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    void Add(RefreshToken token);

    /// <summary>
    /// Loads every currently-active (not revoked, not expired) refresh token for a user, tracked so the
    /// caller can call <see cref="RefreshToken.Revoke"/> on each and have EF persist it on the next
    /// <c>SaveChangesAsync</c> — used to sign a user's other sessions out after a password change.
    /// </summary>
    Task<List<RefreshToken>> GetActiveByUserIdAsync(UserId userId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
