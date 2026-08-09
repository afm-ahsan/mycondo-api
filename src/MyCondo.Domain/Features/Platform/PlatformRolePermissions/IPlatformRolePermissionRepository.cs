using MyCondo.Domain.Features.Platform.PlatformRoles;

namespace MyCondo.Domain.Features.Platform.PlatformRolePermissions;

public interface IPlatformRolePermissionRepository
{
    Task<List<string>> GetPermissionNamesForRoleAsync(PlatformRoleId roleId, CancellationToken cancellationToken);

    void Add(PlatformRolePermission platformRolePermission);
}
