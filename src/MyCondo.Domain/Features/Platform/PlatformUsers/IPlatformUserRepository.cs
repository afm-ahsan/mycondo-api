using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Platform.PlatformUsers;

public interface IPlatformUserRepository
{
    Task<PlatformUser?> GetByIdAsync(PlatformUserId id, CancellationToken cancellationToken);

    /// <summary>Global lookup — platform identities have no tenant to scope by.</summary>
    Task<PlatformUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> AnyAsync(CancellationToken cancellationToken);

    /// <summary>Search/filter/paginate Platform users for the Platform User Administration list. Global —
    /// no tenant to scope by, unlike <see cref="MyCondo.Domain.Features.Identity.Users.IUserRepository.SearchAsync"/>.</summary>
    Task<PagedResult<PlatformUser>> SearchAsync(
        string? searchText,
        PlatformUserStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(PlatformUser platformUser);
}
