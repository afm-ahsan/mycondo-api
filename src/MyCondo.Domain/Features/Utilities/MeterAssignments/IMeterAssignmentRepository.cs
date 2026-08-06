using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Domain.Features.Utilities.MeterAssignments;

public interface IMeterAssignmentRepository
{
    Task<MeterAssignment?> GetOpenForMeterAsync(Guid tenantId, MeterId meterId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MeterAssignment>> GetHistoryForMeterAsync(
        Guid tenantId, MeterId meterId, CancellationToken cancellationToken);

    void Add(MeterAssignment assignment);
}
