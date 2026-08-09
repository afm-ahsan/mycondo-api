using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Residents;

/// <summary>
/// The shared party record for owners/occupants/family members of a flat — the "resident" identity
/// other registers link to instead of duplicating name/phone/email. Deliberately does not model
/// fractional ownership percentages or lease terms yet (kickoff.md's "Resident Management" module
/// scopes `Ownership`/`Lease` as materially larger, separately-approved features — deferred, not
/// forgotten). Guests, domestic workers, service providers, and staff are NOT residents and get their
/// own lightweight profile entities in later slices — they don't belong on this aggregate.
/// </summary>
public sealed class Resident : AggregateRoot<ResidentId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public FlatId FlatId { get; private set; }
    public string FullName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public ResidentType ResidentType { get; private set; }

    /// <summary>
    /// Bridges this party record to a portal <see cref="Identity.Users.User"/> account (Phase 3,
    /// mycondo-docs ADR-021) — null for every resident until an admin explicitly links one. Never set
    /// automatically: no email/phone/name matching, no guessing. Existing residents with no portal
    /// account remain valid indefinitely with this left null; nothing in the system requires it.
    /// </summary>
    public Guid? UserId { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }

    private Resident()
    {
        FullName = null!;
    }

    private Resident(
        ResidentId id,
        Guid tenantId,
        FlatId flatId,
        string fullName,
        string? phone,
        string? email,
        ResidentType residentType,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FlatId = flatId;
        FullName = fullName;
        Phone = phone;
        Email = email;
        ResidentType = residentType;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Resident Register(
        Guid tenantId,
        FlatId flatId,
        string fullName,
        string? phone,
        string? email,
        ResidentType residentType,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new Resident(
            ResidentId.New(), tenantId, flatId, fullName.Trim(), phone?.Trim(), email?.Trim(),
            residentType, nowUtc);
    }

    public void UpdateContactDetails(string? phone, string? email)
    {
        Phone = phone?.Trim();
        Email = email?.Trim();
        Version++;
    }

    /// <summary>Explicit admin action bridging this resident record to a portal User account — the
    /// caller (LinkResidentToUserCommandHandler) is responsible for having already verified the User
    /// belongs to the same Tenant; this method only guards against a no-op re-link.</summary>
    public void LinkToUser(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (UserId == userId)
        {
            return;
        }

        UserId = userId;
        Version++;
    }

    /// <summary>Picked up by <c>ResidentConfiguration</c>'s <c>DeletedAtUtc == null</c> query filter.</summary>
    public void Deactivate(DateTimeOffset nowUtc, Guid? deactivatedBy)
    {
        if (DeletedAtUtc is not null)
        {
            return;
        }

        DeletedAtUtc = nowUtc;
        DeletedBy = deactivatedBy;
    }
}
