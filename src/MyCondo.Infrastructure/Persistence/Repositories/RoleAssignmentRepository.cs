using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Identity.RoleAssignments;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class RoleAssignmentRepository(MyCondoDbContext db) : IRoleAssignmentRepository
{
    public Task<bool> ExistsAsync(
        Guid tenantId,
        UserId userId,
        RoleId roleId,
        Guid? buildingId,
        CancellationToken cancellationToken) =>
        db.Set<RoleAssignment>().AnyAsync(
            a => a.TenantId == tenantId
                 && a.UserId == userId
                 && a.RoleId == roleId
                 && a.BuildingId == buildingId,
            cancellationToken);

    public Task<RoleAssignment?> GetAsync(
        Guid tenantId,
        UserId userId,
        RoleId roleId,
        Guid? buildingId,
        CancellationToken cancellationToken) =>
        db.Set<RoleAssignment>().FirstOrDefaultAsync(
            a => a.TenantId == tenantId
                 && a.UserId == userId
                 && a.RoleId == roleId
                 && a.BuildingId == buildingId,
            cancellationToken);

    public Task<int> CountTenantWideHoldersAsync(
        Guid tenantId, RoleId roleId, CancellationToken cancellationToken) =>
        db.Set<RoleAssignment>()
          .Where(a => a.TenantId == tenantId && a.RoleId == roleId && a.BuildingId == null)
          .Select(a => a.UserId)
          .Distinct()
          .CountAsync(cancellationToken);

    public Task<List<RoleAssignment>> GetForRoleAsync(
        Guid tenantId, RoleId roleId, CancellationToken cancellationToken) =>
        db.Set<RoleAssignment>()
          .Where(a => a.TenantId == tenantId && a.RoleId == roleId)
          .ToListAsync(cancellationToken);

    public Task<List<RoleAssignment>> GetForUserAsync(
        Guid tenantId, UserId userId, CancellationToken cancellationToken) =>
        db.Set<RoleAssignment>()
          .Where(a => a.TenantId == tenantId && a.UserId == userId)
          .ToListAsync(cancellationToken);

    public void Add(RoleAssignment roleAssignment) => db.Set<RoleAssignment>().Add(roleAssignment);

    public void Remove(RoleAssignment roleAssignment) => db.Set<RoleAssignment>().Remove(roleAssignment);
}
