namespace MyCondo.Domain.Features.Platform.PlatformRefreshTokens;

public interface IPlatformRefreshTokenRepository
{
    Task<PlatformRefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(PlatformRefreshToken refreshToken);
}
