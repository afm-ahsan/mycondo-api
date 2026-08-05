using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Domain.Features.Security.DomesticWorkerAssignments;

public interface IDomesticWorkerAssignmentRepository
{
    Task<DomesticWorkerAssignment?> GetByIdAsync(DomesticWorkerAssignmentId id, CancellationToken cancellationToken);

    Task<List<DomesticWorkerAssignment>> GetForWorkerAsync(
        Guid tenantId, DomesticWorkerProfileId workerId, CancellationToken cancellationToken);

    void Add(DomesticWorkerAssignment assignment);
}
