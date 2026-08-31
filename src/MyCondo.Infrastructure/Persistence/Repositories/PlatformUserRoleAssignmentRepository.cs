using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Platform.PlatformRoles;
using MyCondo.Domain.Features.Platform.PlatformUserRoleAssignments;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class PlatformUserRoleAssignmentRepository(MyCondoDbContext db) : IPlatformUserRoleAssignmentRepository
{
    public Task<List<PlatformUserRoleAssignment>> GetForUserAsync(
        PlatformUserId platformUserId, CancellationToken cancellationToken) =>
        db.Set<PlatformUserRoleAssignment>()
          .AsNoTracking()
          .Where(a => a.PlatformUserId == platformUserId)
          .ToListAsync(cancellationToken);

    public Task<List<PlatformUserRoleAssignment>> GetForRoleAsync(
        PlatformRoleId platformRoleId, CancellationToken cancellationToken) =>
        db.Set<PlatformUserRoleAssignment>()
          .AsNoTracking()
          .Where(a => a.PlatformRoleId == platformRoleId)
          .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(
        PlatformUserId platformUserId, PlatformRoleId platformRoleId, CancellationToken cancellationToken) =>
        db.Set<PlatformUserRoleAssignment>()
          .AnyAsync(a => a.PlatformUserId == platformUserId && a.PlatformRoleId == platformRoleId, cancellationToken);

    public async Task<int> LockAndCountActiveSuperAdminHoldersAsync(
        PlatformRoleId platformRoleId, CancellationToken cancellationToken)
    {
        List<Guid> holderPlatformUserIds = await db.Database
            .SqlQuery<Guid>($"""
                SELECT a.platform_user_id AS "Value"
                FROM platform.platform_user_role_assignments a
                JOIN platform.platform_users u ON u.id = a.platform_user_id
                WHERE a.platform_role_id = {platformRoleId.Value} AND u.status = 0
                FOR UPDATE OF a, u
                """)
            .ToListAsync(cancellationToken);

        return holderPlatformUserIds.Distinct().Count();
    }

    public void Add(PlatformUserRoleAssignment assignment) =>
        db.Set<PlatformUserRoleAssignment>().Add(assignment);

    public void Remove(PlatformUserRoleAssignment assignment) =>
        db.Set<PlatformUserRoleAssignment>().Remove(assignment);
}
