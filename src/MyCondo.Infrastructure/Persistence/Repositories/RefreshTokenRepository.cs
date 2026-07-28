using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.RefreshTokens;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(MyCondoDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.Set<RefreshToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public void Add(RefreshToken token) => db.Set<RefreshToken>().Add(token);
}
