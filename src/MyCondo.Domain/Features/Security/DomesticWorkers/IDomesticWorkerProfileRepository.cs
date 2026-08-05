using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Security.DomesticWorkers;

public interface IDomesticWorkerProfileRepository
{
    Task<DomesticWorkerProfile?> GetByIdAsync(DomesticWorkerProfileId id, CancellationToken cancellationToken);

    Task<PagedResult<DomesticWorkerProfile>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(DomesticWorkerProfile profile);
}
