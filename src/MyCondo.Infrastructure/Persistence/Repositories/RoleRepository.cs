using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(MyCondoDbContext db) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken) =>
        db.Set<Role>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Role?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken) =>
        db.Set<Role>().FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == name, cancellationToken);

    public Task<List<Role>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<Role>().Where(r => r.TenantId == tenantId).OrderBy(r => r.Name).ToListAsync(cancellationToken);

    public void Add(Role role) => db.Set<Role>().Add(role);
}
