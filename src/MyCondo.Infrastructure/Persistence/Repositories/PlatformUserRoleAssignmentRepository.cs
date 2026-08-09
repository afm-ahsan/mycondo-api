using Microsoft.EntityFrameworkCore;
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

    public void Add(PlatformUserRoleAssignment assignment) =>
        db.Set<PlatformUserRoleAssignment>().Add(assignment);
}
