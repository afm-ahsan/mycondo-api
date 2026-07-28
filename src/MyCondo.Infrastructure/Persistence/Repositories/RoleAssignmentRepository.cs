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

    public void Add(RoleAssignment roleAssignment) => db.Set<RoleAssignment>().Add(roleAssignment);
}
