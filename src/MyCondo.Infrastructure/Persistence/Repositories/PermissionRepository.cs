using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.Permissions;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PermissionRepository(MyCondoDbContext db) : IPermissionRepository
{
    public Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Set<Permission>().AsNoTracking().OrderBy(p => p.Name).ToListAsync(cancellationToken);

    public Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken cancellationToken) =>
        db.Set<Permission>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(PermissionId id, CancellationToken cancellationToken) =>
        db.Set<Permission>().AnyAsync(p => p.Id == id, cancellationToken);
}
