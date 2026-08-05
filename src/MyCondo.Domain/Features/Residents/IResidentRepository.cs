using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Residents;

public interface IResidentRepository
{
    Task<Resident?> GetByIdAsync(ResidentId id, CancellationToken cancellationToken);

    Task<PagedResult<Resident>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Resident resident);
}
