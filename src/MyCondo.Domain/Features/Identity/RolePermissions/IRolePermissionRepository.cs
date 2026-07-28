using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.Roles;

namespace MyCondo.Domain.Features.Identity.RolePermissions;

public interface IRolePermissionRepository
{
    Task<bool> ExistsAsync(RoleId roleId, PermissionId permissionId, CancellationToken cancellationToken);
    void Add(RolePermission rolePermission);
}
