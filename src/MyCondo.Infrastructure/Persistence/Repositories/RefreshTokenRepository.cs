using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.RefreshTokens;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(MyCondoDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public void Add(RefreshToken token) => db.Set<RefreshToken>().Add(token);

    public Task<List<RefreshToken>> GetActiveByUserIdAsync(
        UserId userId, DateTimeOffset nowUtc, CancellationToken cancellationToken) =>
        db.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);
}
