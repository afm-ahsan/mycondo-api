using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Platform.PlatformRolePermissions;
using MyCondo.Domain.Features.Platform.PlatformRoles;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PlatformRolePermissionRepository(MyCondoDbContext db) : IPlatformRolePermissionRepository
{
    public async Task<List<string>> GetPermissionNamesForRoleAsync(
        PlatformRoleId roleId, CancellationToken cancellationToken) =>
        await (
            from rp in db.Set<PlatformRolePermission>().AsNoTracking()
            where rp.PlatformRoleId == roleId
            join p in db.Set<Permission>().AsNoTracking() on rp.PermissionId equals p.Id
            select p.Name
        ).Distinct().ToListAsync(cancellationToken);

    public void Add(PlatformRolePermission platformRolePermission) =>
        db.Set<PlatformRolePermission>().Add(platformRolePermission);
}
