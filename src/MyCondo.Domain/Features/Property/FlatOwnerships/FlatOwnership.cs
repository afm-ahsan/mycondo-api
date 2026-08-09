using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Property.FlatOwnerships;

/// <summary>
/// A resource-relationship record (Phase 3, mycondo-docs ADR-021) linking a portal <c>User</c> directly
/// to a <c>Flat</c> they own — deliberately NOT modeled as a column on <see cref="Flat"/> or routed
/// through <see cref="Residents.Resident"/> (ownership is a legal/financial relationship, independent of
/// whether the owner is ever recorded as an on-site occupant). Multiple active rows for the same Flat
/// represent co-ownership; multiple active rows for the same User represent owning several Flats —
/// neither is a special case, both are just "more than one row," matching the target model's explicit
/// requirement to support co-owners and multi-Flat owners without a schema that assumes one owner per
/// Flat forever. No <c>OwnershipType</c>/percentage field — Phase 3 doesn't need it and the suggested
/// model treats it as optional; add it later if a real requirement needs it.
/// </summary>
public sealed class FlatOwnership : AggregateRoot<FlatOwnershipId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public FlatId FlatId { get; private set; }
    public FlatOwnershipStatus Status { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private FlatOwnership() { }

    private FlatOwnership(
        FlatOwnershipId id,
        Guid tenantId,
        Guid userId,
        FlatId flatId,
        DateOnly startDate,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        FlatId = flatId;
        Status = FlatOwnershipStatus.Active;
        StartDate = startDate;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static FlatOwnership Grant(
        Guid tenantId, Guid userId, FlatId flatId, DateOnly startDate, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        return new FlatOwnership(FlatOwnershipId.New(), tenantId, userId, flatId, startDate, nowUtc);
    }

    /// <summary>Ends the ownership relationship (transfer, sale, correction). Does not delete the row —
    /// history is preserved so a future ownership-transfer audit trail has something to read.</summary>
    public void End(DateOnly endDate, DateTimeOffset nowUtc)
    {
        if (Status == FlatOwnershipStatus.Ended)
        {
            return;
        }

        Status = FlatOwnershipStatus.Ended;
        EndDate = endDate;
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
