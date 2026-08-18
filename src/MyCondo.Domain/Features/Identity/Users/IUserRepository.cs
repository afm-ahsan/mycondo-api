using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Identity.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Guid tenantId, string email, CancellationToken cancellationToken);

    /// <summary>Used to detect "first user of this tenant" for the OrganizationAdmin bootstrap in
    /// RegisterUserCommandHandler.</summary>
    Task<bool> AnyForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<List<User>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Batched lookup for resolving display names (e.g. audit/custody "performed by" actors)
    /// without one query per row. Missing/foreign-tenant ids are simply absent from the result.</summary>
    Task<List<User>> GetByIdsAsync(Guid tenantId, IReadOnlyCollection<UserId> ids, CancellationToken cancellationToken);

    /// <summary>Search/filter/paginate the tenant's users for the User Administration list. Filters
    /// only by users who hold at least one assignment of <paramref name="roleId"/> when provided.</summary>
    Task<PagedResult<User>> SearchAsync(
        Guid tenantId,
        string? searchText,
        Guid? roleId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(User user);
}
