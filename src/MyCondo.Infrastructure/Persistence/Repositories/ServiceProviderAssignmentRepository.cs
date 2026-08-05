using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Security.ServiceProviderAssignments;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ServiceProviderAssignmentRepository(MyCondoDbContext db) : IServiceProviderAssignmentRepository
{
    public Task<ServiceProviderAssignment?> GetByIdAsync(ServiceProviderAssignmentId id, CancellationToken cancellationToken) =>
        db.Set<ServiceProviderAssignment>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<List<ServiceProviderAssignment>> GetForProviderAsync(
        Guid tenantId, ServiceProviderProfileId providerId, CancellationToken cancellationToken) =>
        db.Set<ServiceProviderAssignment>()
            .Where(a => a.TenantId == tenantId && a.ServiceProviderProfileId == providerId)
            .OrderByDescending(a => a.ValidFromUtc)
            .ToListAsync(cancellationToken);

    public void Add(ServiceProviderAssignment assignment) => db.Set<ServiceProviderAssignment>().Add(assignment);
}
