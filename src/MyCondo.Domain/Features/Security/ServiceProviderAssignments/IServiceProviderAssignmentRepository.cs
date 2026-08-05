using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Domain.Features.Security.ServiceProviderAssignments;

public interface IServiceProviderAssignmentRepository
{
    Task<ServiceProviderAssignment?> GetByIdAsync(ServiceProviderAssignmentId id, CancellationToken cancellationToken);

    Task<List<ServiceProviderAssignment>> GetForProviderAsync(
        Guid tenantId, ServiceProviderProfileId providerId, CancellationToken cancellationToken);

    void Add(ServiceProviderAssignment assignment);
}
