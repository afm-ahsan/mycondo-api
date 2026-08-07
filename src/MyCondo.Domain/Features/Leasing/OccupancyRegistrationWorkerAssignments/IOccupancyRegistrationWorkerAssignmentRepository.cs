using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;

public interface IOccupancyRegistrationWorkerAssignmentRepository
{
    Task<OccupancyRegistrationWorkerAssignment?> GetByIdAsync(
        OccupancyRegistrationWorkerAssignmentId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OccupancyRegistrationWorkerAssignment>> GetForRegistrationAsync(
        OccupancyRegistrationId occupancyRegistrationId, CancellationToken cancellationToken);

    void Add(OccupancyRegistrationWorkerAssignment assignment);
}
