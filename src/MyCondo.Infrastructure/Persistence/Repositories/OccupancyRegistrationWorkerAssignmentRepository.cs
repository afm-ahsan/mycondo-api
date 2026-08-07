using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class OccupancyRegistrationWorkerAssignmentRepository(MyCondoDbContext db)
    : IOccupancyRegistrationWorkerAssignmentRepository
{
    public Task<OccupancyRegistrationWorkerAssignment?> GetByIdAsync(
        OccupancyRegistrationWorkerAssignmentId id, CancellationToken cancellationToken) =>
        db.Set<OccupancyRegistrationWorkerAssignment>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OccupancyRegistrationWorkerAssignment>> GetForRegistrationAsync(
        OccupancyRegistrationId occupancyRegistrationId, CancellationToken cancellationToken) =>
        await db.Set<OccupancyRegistrationWorkerAssignment>()
            .Where(x => x.OccupancyRegistrationId == occupancyRegistrationId)
            .OrderByDescending(x => x.AssignedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(OccupancyRegistrationWorkerAssignment assignment) =>
        db.Set<OccupancyRegistrationWorkerAssignment>().Add(assignment);
}
