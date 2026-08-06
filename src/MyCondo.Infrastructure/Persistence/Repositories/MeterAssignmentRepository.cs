using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Utilities.MeterAssignments;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class MeterAssignmentRepository(MyCondoDbContext db) : IMeterAssignmentRepository
{
    public Task<MeterAssignment?> GetOpenForMeterAsync(Guid tenantId, MeterId meterId, CancellationToken cancellationToken) =>
        db.Set<MeterAssignment>().FirstOrDefaultAsync(
            a => a.TenantId == tenantId && a.MeterId == meterId && a.AssignedToUtc == null, cancellationToken);

    public async Task<IReadOnlyList<MeterAssignment>> GetHistoryForMeterAsync(
        Guid tenantId, MeterId meterId, CancellationToken cancellationToken) =>
        await db.Set<MeterAssignment>()
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.MeterId == meterId)
            .OrderByDescending(a => a.AssignedFromUtc)
            .ToListAsync(cancellationToken);

    public void Add(MeterAssignment assignment) => db.Set<MeterAssignment>().Add(assignment);
}
