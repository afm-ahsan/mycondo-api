namespace MyCondo.Domain.Features.Identity.RefreshTokens;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    void Add(RefreshToken token);
}
