namespace MyCondo.Domain.Features.Platform.PlatformUsers;

public interface IPlatformUserRepository
{
    Task<PlatformUser?> GetByIdAsync(PlatformUserId id, CancellationToken cancellationToken);

    /// <summary>Global lookup — platform identities have no tenant to scope by.</summary>
    Task<PlatformUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> AnyAsync(CancellationToken cancellationToken);

    void Add(PlatformUser platformUser);
}
