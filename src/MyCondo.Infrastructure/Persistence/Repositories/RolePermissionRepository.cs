using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class RolePermissionRepository(MyCondoDbContext db) : IRolePermissionRepository
{
    public Task<bool> ExistsAsync(RoleId roleId, PermissionId permissionId, CancellationToken cancellationToken) =>
        db.Set<RolePermission>()
          .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

    public Task<RolePermission?> GetAsync(RoleId roleId, PermissionId permissionId, CancellationToken cancellationToken) =>
        db.Set<RolePermission>()
          .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

    public void Add(RolePermission rolePermission) => db.Set<RolePermission>().Add(rolePermission);

    public void Remove(RolePermission rolePermission) => db.Set<RolePermission>().Remove(rolePermission);
}
