namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Centralizes "does this User have an active resource relationship with this Flat" (Phase 3,
/// mycondo-docs ADR-021) — the one place FlatOwnership/active-Occupancy lookups happen, so handlers
/// never scatter ad-hoc ownership/occupancy comparisons. Deliberately returns only relationship facts,
/// never a permission decision: callers still combine the result with
/// <see cref="ICurrentUserProvider.HasPermissionForBuilding"/> themselves — role/permission and resource
/// relationship must never substitute for one another (see the Phase-3 spec's "Role + Relationship
/// Defense in Depth").
/// </summary>
public interface IFlatAccessAuthorizer
{
    Task<bool> HasActiveOwnershipAsync(Guid tenantId, Guid userId, Guid flatId, CancellationToken cancellationToken);

    Task<bool> HasActiveOccupancyAsync(Guid tenantId, Guid userId, Guid flatId, CancellationToken cancellationToken);

    /// <summary>Every active ownership and occupancy relationship a User currently holds, across every
    /// Flat — the building block for self-service "my flats"/"my invoices" style queries. Callers must
    /// still check <see cref="ICurrentUserProvider.HasPermissionForBuilding"/> per relationship before
    /// exposing it — this method reports relationships, not access decisions.</summary>
    Task<List<FlatRelationship>> GetActiveRelationshipsAsync(
        Guid tenantId, Guid userId, CancellationToken cancellationToken);
}

public enum FlatRelationshipKind
{
    Ownership,
    Occupancy,
}

public sealed record FlatRelationship(
    Guid FlatId,
    Guid BuildingId,
    FlatRelationshipKind Kind,
    DateOnly? StartDate,
    DateOnly? EndDate);
