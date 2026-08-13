using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Domain.Features.Property.FlatOwnerships;

/// <summary>
/// A resource-relationship record (Phase 3, mycondo-docs ADR-021) linking a <see cref="Resident"/>
/// party record to a <c>Flat</c> they own — deliberately NOT modeled as a column on <see cref="Flat"/>.
/// Ownership is a legal/financial relationship, independent of whether the owner is ever recorded as an
/// on-site occupant, and independent of whether the owner has a portal login: this references
/// <see cref="ResidentId"/>, not a portal <c>User</c>, so an owner's profile can be recorded (Flat Owner
/// Registration) without requiring a User account to exist first. Self-service access for an owner who
/// *does* have a portal account is resolved by <c>IFlatAccessAuthorizer</c> via the Resident's optional
/// <see cref="Resident.UserId"/> link, not by this record directly.
/// Multiple active rows for the same Flat represent co-ownership; multiple active rows for the same
/// Resident represent owning several Flats — neither is a special case, both are just "more than one
/// row," matching the target model's explicit requirement to support co-owners and multi-Flat owners
/// without a schema that assumes one owner per Flat forever. No <c>OwnershipType</c>/percentage field —
/// not needed yet; add it later if a real requirement needs it.
/// </summary>
public sealed class FlatOwnership : AggregateRoot<FlatOwnershipId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid ResidentId { get; private set; }
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
        Guid residentId,
        FlatId flatId,
        DateOnly startDate,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        ResidentId = residentId;
        FlatId = flatId;
        Status = FlatOwnershipStatus.Active;
        StartDate = startDate;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static FlatOwnership Grant(
        Guid tenantId, Guid residentId, FlatId flatId, DateOnly startDate, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (residentId == Guid.Empty)
        {
            throw new ArgumentException("ResidentId is required.", nameof(residentId));
        }

        return new FlatOwnership(FlatOwnershipId.New(), tenantId, residentId, flatId, startDate, nowUtc);
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
