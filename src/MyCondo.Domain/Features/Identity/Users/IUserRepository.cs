namespace MyCondo.Domain.Features.Identity.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Guid tenantId, string email, CancellationToken cancellationToken);
    void Add(User user);
}
