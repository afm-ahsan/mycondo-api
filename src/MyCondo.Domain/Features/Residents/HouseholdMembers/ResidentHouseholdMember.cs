using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Residents.HouseholdMembers;

/// <summary>
/// A family member (Father/Mother/Spouse/Child) recorded against the primary Flat Owner's shared
/// <see cref="Resident"/> party record — the Owner-side counterpart to
/// <see cref="Leasing.HouseholdMembers.HouseholdMember"/>, kept as a separate entity/table rather than a
/// shared one because Owner household membership is keyed by <see cref="ResidentId"/> while Tenant
/// household membership is keyed by an <c>OccupancyRegistrationId</c> — two structurally similar but
/// independently-owned concepts, matching this codebase's established child-entity convention (own
/// repository, add/update/deactivate independently without reloading the owning aggregate).
/// </summary>
public sealed class ResidentHouseholdMember : Entity<ResidentHouseholdMemberId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid ResidentId { get; private set; }
    public string FullName { get; private set; }
    public RelationshipType RelationshipType { get; private set; }
    public string Gender { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string? NationalIdNumber { get; private set; }
    public string? BirthCertificateNumber { get; private set; }
    public string? BloodGroup { get; private set; }
    public string? Religion { get; private set; }
    public string? Nationality { get; private set; }
    public string? Occupation { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? PrimaryPhotoAttachmentId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private ResidentHouseholdMember()
    {
        FullName = null!;
        Gender = null!;
    }

    private ResidentHouseholdMember(
        ResidentHouseholdMemberId id, Guid tenantId, Guid residentId, string fullName,
        RelationshipType relationshipType, string gender, DateOnly dateOfBirth, string? nationalIdNumber,
        string? birthCertificateNumber, string? bloodGroup, string? religion, string? nationality,
        string? occupation, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        ResidentId = residentId;
        FullName = fullName;
        RelationshipType = relationshipType;
        Gender = gender;
        DateOfBirth = dateOfBirth;
        NationalIdNumber = nationalIdNumber;
        BirthCertificateNumber = birthCertificateNumber;
        BloodGroup = bloodGroup;
        Religion = religion;
        Nationality = nationality;
        Occupation = occupation;
        IsActive = true;
        CreatedAtUtc = nowUtc;
    }

    private static void EnsureValid(
        Guid tenantId, Guid residentId, string fullName, RelationshipType relationshipType, string gender,
        DateOnly dateOfBirth, string? nationalIdNumber, string? birthCertificateNumber, DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (residentId == Guid.Empty)
        {
            throw new ArgumentException("ResidentId is required.", nameof(residentId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(gender);

        if (dateOfBirth > DateOnly.FromDateTime(nowUtc.UtcDateTime))
        {
            throw new ArgumentException("Date of birth cannot be in the future.", nameof(dateOfBirth));
        }

        if (relationshipType == RelationshipType.Child
            && string.IsNullOrWhiteSpace(nationalIdNumber) && string.IsNullOrWhiteSpace(birthCertificateNumber))
        {
            throw new ArgumentException(
                "A Child household member requires either a National ID or a Birth Certificate number.",
                nameof(nationalIdNumber));
        }
    }

    public static ResidentHouseholdMember Add(
        Guid tenantId, Guid residentId, string fullName, RelationshipType relationshipType, string gender,
        DateOnly dateOfBirth, string? nationalIdNumber, string? birthCertificateNumber, string? bloodGroup,
        string? religion, string? nationality, string? occupation, DateTimeOffset nowUtc)
    {
        EnsureValid(
            tenantId, residentId, fullName, relationshipType, gender, dateOfBirth, nationalIdNumber,
            birthCertificateNumber, nowUtc);

        return new ResidentHouseholdMember(
            ResidentHouseholdMemberId.New(), tenantId, residentId, fullName.Trim(), relationshipType, gender.Trim(),
            dateOfBirth, nationalIdNumber?.Trim(), birthCertificateNumber?.Trim(), bloodGroup?.Trim(),
            religion?.Trim(), nationality?.Trim(), occupation?.Trim(), nowUtc);
    }

    /// <summary><paramref name="nationalIdNumber"/>/<paramref name="birthCertificateNumber"/> follow the
    /// same "empty submission means not retyped, not cleared" convention as
    /// <see cref="Residents.Resident.UpdateOwnerDetails"/> — both are masked on every read, so the
    /// client can never round-trip the existing value back through an edit form.</summary>
    public void Update(
        string fullName, RelationshipType relationshipType, string gender, DateOnly dateOfBirth,
        string? nationalIdNumber, string? birthCertificateNumber, string? bloodGroup, string? religion,
        string? nationality, string? occupation, DateTimeOffset nowUtc)
    {
        string? effectiveNationalIdNumber = string.IsNullOrWhiteSpace(nationalIdNumber)
            ? NationalIdNumber : nationalIdNumber.Trim();
        string? effectiveBirthCertificateNumber = string.IsNullOrWhiteSpace(birthCertificateNumber)
            ? BirthCertificateNumber : birthCertificateNumber.Trim();

        EnsureValid(
            TenantId, ResidentId, fullName, relationshipType, gender, dateOfBirth, effectiveNationalIdNumber,
            effectiveBirthCertificateNumber, nowUtc);

        FullName = fullName.Trim();
        RelationshipType = relationshipType;
        Gender = gender.Trim();
        DateOfBirth = dateOfBirth;
        NationalIdNumber = effectiveNationalIdNumber;
        BirthCertificateNumber = effectiveBirthCertificateNumber;
        BloodGroup = bloodGroup?.Trim();
        Religion = religion?.Trim();
        Nationality = nationality?.Trim();
        Occupation = occupation?.Trim();
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Removes this member from the active household without affecting the primary Resident or
    /// any FlatOwnership — e.g. a family member relocating independently.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    public void SetPrimaryPhoto(Guid? photoAttachmentId)
    {
        PrimaryPhotoAttachmentId = photoAttachmentId;
    }
}
