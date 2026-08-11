using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
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

    public async Task<PagedResult<User>> SearchAsync(
        Guid tenantId,
        string? searchText,
        Guid? roleId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<User> query = db.Set<User>()
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(u =>
                EF.Functions.ILike(u.FullName, $"%{searchText}%")
                || EF.Functions.ILike(u.Email, $"%{searchText}%"));
        }

        if (isActive is bool activeFilter)
        {
            UserStatus status = activeFilter ? UserStatus.Active : UserStatus.Inactive;
            query = query.Where(u => u.Status == status);
        }

        if (roleId is Guid roleIdValue)
        {
            RoleId typedRoleId = new(roleIdValue);
            query = query.Where(u => db.Set<RoleAssignment>()
                .Any(a => a.TenantId == tenantId && a.UserId == u.Id && a.RoleId == typedRoleId));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<User> items = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<User>(items, page, pageSize, total);
    }

    public void Add(User user) => db.Set<User>().Add(user);
}
