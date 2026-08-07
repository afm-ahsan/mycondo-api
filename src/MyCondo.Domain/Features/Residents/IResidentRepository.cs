using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

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

    /// <summary>Exact-name match within a flat, used to reuse an existing party record instead of
    /// creating a duplicate when a new registration names someone already on file for that flat.</summary>
    Task<Resident?> FindByFlatAndNameAsync(
        Guid tenantId, FlatId flatId, string fullName, CancellationToken cancellationToken);

    void Add(Resident resident);
}
