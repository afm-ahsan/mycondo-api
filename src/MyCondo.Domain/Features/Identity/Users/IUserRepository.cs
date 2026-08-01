namespace MyCondo.Domain.Features.Identity.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Guid tenantId, string email, CancellationToken cancellationToken);

    /// <summary>Used to detect "first user of this tenant" for the SuperAdmin bootstrap in
    /// RegisterUserCommandHandler.</summary>
    Task<bool> AnyForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<List<User>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    void Add(User user);
}
