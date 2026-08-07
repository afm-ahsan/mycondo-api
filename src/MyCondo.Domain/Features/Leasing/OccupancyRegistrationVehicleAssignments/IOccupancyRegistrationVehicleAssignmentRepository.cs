using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;

public interface IOccupancyRegistrationVehicleAssignmentRepository
{
    Task<OccupancyRegistrationVehicleAssignment?> GetByIdAsync(
        OccupancyRegistrationVehicleAssignmentId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<OccupancyRegistrationVehicleAssignment>> GetForRegistrationAsync(
        OccupancyRegistrationId occupancyRegistrationId, CancellationToken cancellationToken);

    void Add(OccupancyRegistrationVehicleAssignment assignment);
}
