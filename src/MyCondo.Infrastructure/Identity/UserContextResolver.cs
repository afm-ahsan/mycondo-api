using Microsoft.EntityFrameworkCore;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Infrastructure.Identity;

/// <summary>
/// Resolves a <see cref="User"/> aggregate to its effective roles + permissions + building scope
/// by joining role_assignments → roles → role_permissions → permissions for the user's tenant.
///
/// Permissions are split by the scope of the *granting* assignment: a role granted via a tenant-wide
/// assignment (BuildingId == null) contributes to the tenant-wide set; the same role granted via a
/// building-scoped assignment contributes only to that building's set. A role held both ways
/// contributes to both — that's correct, not a duplicate to dedupe away. See ADR-014.
/// </summary>
public sealed class UserContextResolver(MyCondoDbContext db) : IUserContextResolver
{
    public async Task<AuthenticatedUserDto> ResolveAsync(User user, CancellationToken cancellationToken)
    {
        ResolvedContext context = await ResolveCoreAsync(user, cancellationToken);

        return new AuthenticatedUserDto(
            UserId: user.Id.Value,
            TenantId: user.TenantId,
            Email: user.Email,
            FullName: user.FullName,
            Roles: context.Roles,
            Permissions: context.TenantWidePermissions,
            BuildingIds: context.BuildingIds,
            BuildingPermissions: context.BuildingPermissions);
    }

    public async Task<UserProfileDto> ResolveProfileAsync(User user, CancellationToken cancellationToken)
    {
        ResolvedContext context = await ResolveCoreAsync(user, cancellationToken);

        return new UserProfileDto(
            UserId: user.Id.Value,
            TenantId: user.TenantId,
            Email: user.Email,
            FullName: user.FullName,
            PhoneNumber: user.PhoneNumber,
            CreatedAtUtc: user.CreatedAtUtc,
            LastLoginAtUtc: user.LastLoginAtUtc,
            Roles: context.Roles,
            // Informational profile view: every permission the user holds anywhere, tenant-wide or
            // building-scoped — unlike AuthenticatedUserDto.Permissions, this is not what goes in the
            // JWT and doesn't need to preserve the tenant-wide/building-scoped distinction.
            Permissions: context.AllPermissions);
    }

    private async Task<ResolvedContext> ResolveCoreAsync(User user, CancellationToken ct)
    {
        List<RoleAssignment> assignments = await db.Set<RoleAssignment>()
            .AsNoTracking()
            .Where(a => a.TenantId == user.TenantId && a.UserId == user.Id)
            .ToListAsync(ct);

        if (assignments.Count == 0)
        {
            return new ResolvedContext([], [], [], [], []);
        }

        List<RoleId> roleIds = assignments.Select(a => a.RoleId).Distinct().ToList();

        List<string> roleNames = await db.Set<Role>()
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync(ct);

        var roleGrants = await (
            from rp in db.Set<RolePermission>().AsNoTracking()
            where roleIds.Contains(rp.RoleId)
            join p in db.Set<Permission>().AsNoTracking() on rp.PermissionId equals p.Id
            select new { rp.RoleId, PermissionName = p.Name }
        ).ToListAsync(ct);

        Dictionary<RoleId, List<string>> permissionNamesByRole = roleGrants
            .GroupBy(g => g.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PermissionName).Distinct().ToList());

        HashSet<string> tenantWidePermissions = [];
        foreach (RoleId roleId in assignments.Where(a => a.BuildingId is null).Select(a => a.RoleId).Distinct())
        {
            if (permissionNamesByRole.TryGetValue(roleId, out List<string>? names))
            {
                tenantWidePermissions.UnionWith(names);
            }
        }

        HashSet<BuildingPermissionGrant> buildingPermissions = [];
        foreach (RoleAssignment assignment in assignments.Where(a => a.BuildingId is not null))
        {
            if (permissionNamesByRole.TryGetValue(assignment.RoleId, out List<string>? names))
            {
                foreach (string name in names)
                {
                    buildingPermissions.Add(new BuildingPermissionGrant(assignment.BuildingId!.Value, name));
                }
            }
        }

        List<Guid> buildingIds = assignments
            .Where(a => a.BuildingId is not null)
            .Select(a => a.BuildingId!.Value)
            .Distinct()
            .ToList();

        List<string> allPermissions = tenantWidePermissions
            .Concat(buildingPermissions.Select(bp => bp.Permission))
            .Distinct()
            .ToList();

        return new ResolvedContext(
            roleNames,
            tenantWidePermissions.ToList(),
            allPermissions,
            buildingIds,
            buildingPermissions.ToList());
    }

    private sealed record ResolvedContext(
        List<string> Roles,
        List<string> TenantWidePermissions,
        List<string> AllPermissions,
        List<Guid> BuildingIds,
        List<BuildingPermissionGrant> BuildingPermissions);
}
