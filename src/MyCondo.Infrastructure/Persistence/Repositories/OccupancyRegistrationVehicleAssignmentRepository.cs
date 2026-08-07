using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class OccupancyRegistrationVehicleAssignmentRepository(MyCondoDbContext db)
    : IOccupancyRegistrationVehicleAssignmentRepository
{
    public Task<OccupancyRegistrationVehicleAssignment?> GetByIdAsync(
        OccupancyRegistrationVehicleAssignmentId id, CancellationToken cancellationToken) =>
        db.Set<OccupancyRegistrationVehicleAssignment>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OccupancyRegistrationVehicleAssignment>> GetForRegistrationAsync(
        OccupancyRegistrationId occupancyRegistrationId, CancellationToken cancellationToken) =>
        await db.Set<OccupancyRegistrationVehicleAssignment>()
            .Where(x => x.OccupancyRegistrationId == occupancyRegistrationId)
            .OrderByDescending(x => x.AssignedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(OccupancyRegistrationVehicleAssignment assignment) =>
        db.Set<OccupancyRegistrationVehicleAssignment>().Add(assignment);
}
