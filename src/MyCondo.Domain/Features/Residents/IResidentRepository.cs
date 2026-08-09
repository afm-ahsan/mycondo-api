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

    /// <summary>Every resident party record bridged to a given portal User (Phase 3, mycondo-docs
    /// ADR-021) — a User can be resident of record for more than one Flat (e.g. owner-occupier in one,
    /// primary occupant in another), so this returns a list, not a single row.</summary>
    Task<List<Resident>> GetByUserIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);

    void Add(Resident resident);
}
