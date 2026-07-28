using Microsoft.EntityFrameworkCore;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Infrastructure.Identity;

/// <summary>
/// Resolves a <see cref="User"/> aggregate to its effective roles + permissions + building scope
/// by joining role_assignments → roles → role_permissions → permissions for the user's tenant.
/// </summary>
public sealed class UserContextResolver(MyCondoDbContext db) : IUserContextResolver
{
    public async Task<AuthenticatedUserDto> ResolveAsync(User user, CancellationToken cancellationToken)
    {
        (List<string> roles, List<string> permissions, List<Guid> buildingIds) =
            await ResolveCoreAsync(user, cancellationToken);

        return new AuthenticatedUserDto(
            UserId: user.Id.Value,
            TenantId: user.TenantId,
            Email: user.Email,
            FullName: user.FullName,
            Roles: roles,
            Permissions: permissions,
            BuildingIds: buildingIds);
    }

    public async Task<UserProfileDto> ResolveProfileAsync(User user, CancellationToken cancellationToken)
    {
        (List<string> roles, List<string> permissions, _) =
            await ResolveCoreAsync(user, cancellationToken);

        return new UserProfileDto(
            UserId: user.Id.Value,
            TenantId: user.TenantId,
            Email: user.Email,
            FullName: user.FullName,
            PhoneNumber: user.PhoneNumber,
            CreatedAtUtc: user.CreatedAtUtc,
            LastLoginAtUtc: user.LastLoginAtUtc,
            Roles: roles,
            Permissions: permissions);
    }

    private async Task<(List<string> Roles, List<string> Permissions, List<Guid> BuildingIds)>
        ResolveCoreAsync(User user, CancellationToken ct)
    {
        List<RoleAssignment> assignments = await db.Set<RoleAssignment>()
            .AsNoTracking()
            .Where(a => a.TenantId == user.TenantId && a.UserId == user.Id)
            .ToListAsync(ct);

        if (assignments.Count == 0)
        {
            return ([], [], []);
        }

        var roleIds = assignments.Select(a => a.RoleId).Distinct().ToList();

        List<string> roleNames = await db.Set<Role>()
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync(ct);

        // Effective permissions = union of permissions across all the user's roles.
        List<string> permissionNames = await
            (from rp in db.Set<RolePermission>().AsNoTracking()
             where roleIds.Contains(rp.RoleId)
             join p in db.Set<MyCondo.Domain.Features.Identity.Permissions.Permission>().AsNoTracking()
                 on rp.PermissionId equals p.Id
             select p.Name).Distinct().ToListAsync(ct);

        List<Guid> buildingIds = assignments
            .Where(a => a.BuildingId is not null)
            .Select(a => a.BuildingId!.Value)
            .Distinct()
            .ToList();

        return (roleNames, permissionNames, buildingIds);
    }
}
