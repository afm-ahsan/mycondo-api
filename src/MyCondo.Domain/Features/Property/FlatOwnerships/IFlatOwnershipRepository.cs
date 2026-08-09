using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Property.FlatOwnerships;

public interface IFlatOwnershipRepository
{
    Task<FlatOwnership?> GetByIdAsync(FlatOwnershipId id, CancellationToken cancellationToken);

    /// <summary>Every active ownership relationship a User currently holds, across all Flats — the
    /// building block for self-service "my flats"/"my invoices" and for
    /// IFlatAccessAuthorizer.GetActiveRelationshipsAsync.</summary>
    Task<List<FlatOwnership>> GetActiveForUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);

    /// <summary>All ownership records for a Flat (active and ended) — admin visibility into a Flat's
    /// ownership history, including past/ended relationships.</summary>
    Task<List<FlatOwnership>> GetForFlatAsync(Guid tenantId, FlatId flatId, CancellationToken cancellationToken);

    Task<bool> ExistsActiveForUserAndFlatAsync(
        Guid tenantId, Guid userId, FlatId flatId, CancellationToken cancellationToken);

    void Add(FlatOwnership ownership);
}
