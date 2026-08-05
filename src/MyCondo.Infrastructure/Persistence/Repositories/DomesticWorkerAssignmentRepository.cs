using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Security.DomesticWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class DomesticWorkerAssignmentRepository(MyCondoDbContext db) : IDomesticWorkerAssignmentRepository
{
    public Task<DomesticWorkerAssignment?> GetByIdAsync(DomesticWorkerAssignmentId id, CancellationToken cancellationToken) =>
        db.Set<DomesticWorkerAssignment>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<List<DomesticWorkerAssignment>> GetForWorkerAsync(
        Guid tenantId, DomesticWorkerProfileId workerId, CancellationToken cancellationToken) =>
        db.Set<DomesticWorkerAssignment>()
            .Where(a => a.TenantId == tenantId && a.DomesticWorkerProfileId == workerId)
            .OrderByDescending(a => a.ValidFromUtc)
            .ToListAsync(cancellationToken);

    public void Add(DomesticWorkerAssignment assignment) => db.Set<DomesticWorkerAssignment>().Add(assignment);
}
