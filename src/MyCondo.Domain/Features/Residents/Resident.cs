using MyCondo.Domain.Common;
using MyCondo.Domain.Common.PhoneNumbers;
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

    // Extended profile — populated by Flat Owner Registration (and optionally by other registers);
    // all nullable since Occupant/FamilyMember rows and pre-existing residents never need them.
    public string? AlternatePhone { get; private set; }
    public string? NationalIdNumber { get; private set; }
    public string? PassportNumber { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Gender { get; private set; }
    public string? PresentAddress { get; private set; }
    public string? PermanentAddress { get; private set; }
    public string? FatherName { get; private set; }
    public string? MotherName { get; private set; }
    public string? MaritalStatus { get; private set; }
    public string? Profession { get; private set; }
    public string? Employer { get; private set; }
    public string? OfficeAddress { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }

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
            ResidentId.New(), tenantId, flatId, fullName.Trim(), BangladeshMobileNumber.Normalize(phone), email?.Trim(),
            residentType, nowUtc);
    }

    public void UpdateContactDetails(string? phone, string? email)
    {
        Phone = BangladeshMobileNumber.Normalize(phone);
        Email = email?.Trim();
        Version++;
    }

    /// <summary>Admin edit of this resident's own profile fields — not the ownership relationship
    /// (<c>FlatOwnership</c>) and not the linked User account.</summary>
    public void UpdateProfile(string newFullName, string? phone, string? email, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newFullName);

        FullName = newFullName.Trim();
        Phone = BangladeshMobileNumber.Normalize(phone);
        Email = email?.Trim();
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Sets the extended owner-profile fields captured by Flat Owner Registration (Steps 2-3: identity,
    /// contact, and family/professional details). Kept as a distinct operation from
    /// <see cref="UpdateProfile"/> so the base resident-profile edit used by every register (Residents
    /// directory, Tenant Registration's linked resident, etc.) is never forced to reason about
    /// owner-only fields. <paramref name="nationalIdNumber"/>/<paramref name="passportNumber"/> follow
    /// the same "empty submission means not retyped, not cleared" convention as
    /// OccupancyRegistration.PrimaryNationalIdNumber — masked values are never round-tripped back
    /// through an edit form, so only a non-blank value overwrites what's on file.
    /// </summary>
    public void UpdateOwnerDetails(
        string? alternatePhone, string? nationalIdNumber, string? passportNumber, DateOnly? dateOfBirth,
        string? gender, string? presentAddress, string? permanentAddress, string? fatherName, string? motherName,
        string? maritalStatus, string? profession, string? employer, string? officeAddress,
        string? emergencyContactName, string? emergencyContactPhone, DateTimeOffset nowUtc)
    {
        AlternatePhone = BangladeshMobileNumber.Normalize(alternatePhone);
        if (!string.IsNullOrWhiteSpace(nationalIdNumber))
        {
            NationalIdNumber = nationalIdNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(passportNumber))
        {
            PassportNumber = passportNumber.Trim();
        }

        DateOfBirth = dateOfBirth;
        Gender = gender?.Trim();
        PresentAddress = presentAddress?.Trim();
        PermanentAddress = permanentAddress?.Trim();
        FatherName = fatherName?.Trim();
        MotherName = motherName?.Trim();
        MaritalStatus = maritalStatus?.Trim();
        Profession = profession?.Trim();
        Employer = employer?.Trim();
        OfficeAddress = officeAddress?.Trim();
        EmergencyContactName = emergencyContactName?.Trim();
        EmergencyContactPhone = BangladeshMobileNumber.Normalize(emergencyContactPhone);
        Version++;
        UpdatedAtUtc = nowUtc;
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
