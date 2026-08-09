using Microsoft.EntityFrameworkCore;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Infrastructure.Identity;

/// <summary>
/// Platform-scope analogue of <see cref="UserContextResolver"/>. Simpler by construction: there is no
/// building-scope dimension at Platform level, so this is a flat role → permission resolution with no
/// tenant-wide/building-scoped split to preserve.
/// </summary>
public sealed class PlatformUserContextResolver(MyCondoDbContext db) : IPlatformUserContextResolver
{
    public async Task<PlatformAuthenticatedUserDto> ResolveAsync(
        PlatformUser platformUser, CancellationToken cancellationToken)
    {
        List<PlatformUserRoleAssignment> assignments = await db.Set<PlatformUserRoleAssignment>()
            .AsNoTracking()
            .Where(a => a.PlatformUserId == platformUser.Id)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return new PlatformAuthenticatedUserDto(
                platformUser.Id.Value, platformUser.Email, platformUser.DisplayName, [], []);
        }

        List<PlatformRoleId> roleIds = assignments.Select(a => a.PlatformRoleId).Distinct().ToList();

        List<string> roleNames = await db.Set<PlatformRole>()
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        List<string> roleGrants = await (
            from rp in db.Set<Domain.Features.Platform.PlatformRolePermissions.PlatformRolePermission>().AsNoTracking()
            where roleIds.Contains(rp.PlatformRoleId)
            join p in db.Set<Domain.Features.Identity.Permissions.Permission>().AsNoTracking()
                on rp.PermissionId equals p.Id
            select p.Name
        ).Distinct().ToListAsync(cancellationToken);

        return new PlatformAuthenticatedUserDto(
            platformUser.Id.Value, platformUser.Email, platformUser.DisplayName, roleNames, roleGrants);
    }
}
