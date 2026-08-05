using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Security.ServiceProviders;

public interface IServiceProviderProfileRepository
{
    Task<ServiceProviderProfile?> GetByIdAsync(ServiceProviderProfileId id, CancellationToken cancellationToken);

    Task<PagedResult<ServiceProviderProfile>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(ServiceProviderProfile profile);
}
