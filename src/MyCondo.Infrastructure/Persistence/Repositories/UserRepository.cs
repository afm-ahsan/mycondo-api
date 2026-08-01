using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(MyCondoDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken) =>
        db.Set<User>().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken) =>
        db.Set<User>()
          .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(Guid tenantId, string email, CancellationToken cancellationToken) =>
        db.Set<User>()
          .AnyAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);

    public Task<bool> AnyForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<User>().AnyAsync(u => u.TenantId == tenantId, cancellationToken);

    public Task<List<User>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<User>().Where(u => u.TenantId == tenantId).OrderBy(u => u.Email).ToListAsync(cancellationToken);

    public void Add(User user) => db.Set<User>().Add(user);
}
