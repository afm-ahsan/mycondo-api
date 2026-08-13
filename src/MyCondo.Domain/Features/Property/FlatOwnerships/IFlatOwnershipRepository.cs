using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Property.FlatOwnerships;

public interface IFlatOwnershipRepository
{
    Task<FlatOwnership?> GetByIdAsync(FlatOwnershipId id, CancellationToken cancellationToken);

    /// <summary>Tenant-wide, paginated Flat Owner register — the backing query for the Flat Owners
    /// list page. <paramref name="search"/> matches against the owning Resident's name/phone/email.</summary>
    Task<PagedResult<FlatOwnership>> SearchAsync(
        Guid tenantId,
        string? search,
        FlatOwnershipStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Every active ownership relationship a Resident currently holds, across all Flats — the
    /// building block for admin visibility and IFlatAccessAuthorizer.GetActiveRelationshipsAsync.</summary>
    Task<List<FlatOwnership>> GetActiveForResidentAsync(Guid tenantId, Guid residentId, CancellationToken cancellationToken);

    /// <summary>Every ownership record for a Resident, active and ended — admin visibility into an
    /// owner's full ownership history across all Flats (mirrors <see cref="GetForFlatAsync"/>'s
    /// per-Flat history, pivoted around the Resident instead).</summary>
    Task<List<FlatOwnership>> GetAllForResidentAsync(Guid tenantId, Guid residentId, CancellationToken cancellationToken);

    /// <summary>All ownership records for a Flat (active and ended) — admin visibility into a Flat's
    /// ownership history, including past/ended relationships.</summary>
    Task<List<FlatOwnership>> GetForFlatAsync(Guid tenantId, FlatId flatId, CancellationToken cancellationToken);

    Task<bool> ExistsActiveForResidentAndFlatAsync(
        Guid tenantId, Guid residentId, FlatId flatId, CancellationToken cancellationToken);

    void Add(FlatOwnership ownership);
}
