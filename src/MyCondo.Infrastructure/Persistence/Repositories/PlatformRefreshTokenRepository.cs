using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.PlatformRefreshTokens;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PlatformRefreshTokenRepository(MyCondoDbContext db) : IPlatformRefreshTokenRepository
{
    public Task<PlatformRefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.Set<PlatformRefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public void Add(PlatformRefreshToken refreshToken) => db.Set<PlatformRefreshToken>().Add(refreshToken);
}
