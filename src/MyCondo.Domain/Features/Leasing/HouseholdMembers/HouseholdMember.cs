using MyCondo.Domain.Common;
using MyCondo.Domain.Common.PhoneNumbers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Domain.Features.Leasing.HouseholdMembers;

/// <summary>
/// A family member/co-occupant listed on an <see cref="OccupancyRegistration"/> — deliberately its own
/// entity with its own repository (matching this codebase's established child-entity convention, see
/// e.g. <c>GeneratorFuelReceipt</c>) rather than an EF owned collection, so members can be added/
/// deactivated independently without reloading the whole registration aggregate.
/// </summary>
public sealed class HouseholdMember : Entity<HouseholdMemberId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public OccupancyRegistrationId OccupancyRegistrationId { get; private set; }
    public string FullName { get; private set; }
    public string RelationshipToPrimary { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? Phone { get; private set; }
    public string? NationalIdNumber { get; private set; }
    public string? Gender { get; private set; }
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

    private HouseholdMember()
    {
        FullName = null!;
        RelationshipToPrimary = null!;
    }

    private HouseholdMember(
        HouseholdMemberId id, Guid tenantId, OccupancyRegistrationId occupancyRegistrationId, string fullName,
        string relationshipToPrimary, DateOnly? dateOfBirth, string? phone, string? nationalIdNumber,
        string? gender, string? birthCertificateNumber, string? bloodGroup, string? religion, string? nationality,
        string? occupation, DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        OccupancyRegistrationId = occupancyRegistrationId;
        FullName = fullName;
        RelationshipToPrimary = relationshipToPrimary;
        DateOfBirth = dateOfBirth;
        Phone = phone;
        NationalIdNumber = nationalIdNumber;
        Gender = gender;
        BirthCertificateNumber = birthCertificateNumber;
        BloodGroup = bloodGroup;
        Religion = religion;
        Nationality = nationality;
        Occupation = occupation;
        IsActive = true;
        CreatedAtUtc = nowUtc;
    }

    private static void EnsureChildHasIdentity(
        string relationshipToPrimary, string? nationalIdNumber, string? birthCertificateNumber)
    {
        if (string.Equals(relationshipToPrimary, "Child", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(nationalIdNumber) && string.IsNullOrWhiteSpace(birthCertificateNumber))
        {
            throw new ArgumentException(
                "A Child household member requires either a National ID or a Birth Certificate number.",
                nameof(nationalIdNumber));
        }
    }

    public static HouseholdMember Add(
        Guid tenantId, OccupancyRegistrationId occupancyRegistrationId, string fullName,
        string relationshipToPrimary, DateOnly? dateOfBirth, string? phone, string? nationalIdNumber,
        string? gender, string? birthCertificateNumber, string? bloodGroup, string? religion, string? nationality,
        string? occupation, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(relationshipToPrimary);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        EnsureChildHasIdentity(relationshipToPrimary, nationalIdNumber, birthCertificateNumber);

        return new HouseholdMember(
            HouseholdMemberId.New(), tenantId, occupancyRegistrationId, fullName.Trim(),
            relationshipToPrimary.Trim(), dateOfBirth, BangladeshMobileNumber.Normalize(phone),
            nationalIdNumber?.Trim(), gender?.Trim(), birthCertificateNumber?.Trim(), bloodGroup?.Trim(),
            religion?.Trim(), nationality?.Trim(), occupation?.Trim(), nowUtc);
    }

    /// <summary><paramref name="nationalIdNumber"/>/<paramref name="birthCertificateNumber"/> follow the
    /// same "empty submission means not retyped, not cleared" convention as
    /// <c>OccupancyRegistration.PrimaryNationalIdNumber</c> — both are masked on every read, so the
    /// client can never round-trip the existing value back through an edit form.</summary>
    public void Update(
        string fullName, string relationshipToPrimary, DateOnly? dateOfBirth, string? phone,
        string? nationalIdNumber, string? gender, string? birthCertificateNumber, string? bloodGroup,
        string? religion, string? nationality, string? occupation, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(relationshipToPrimary);

        string? effectiveNationalIdNumber = string.IsNullOrWhiteSpace(nationalIdNumber)
            ? NationalIdNumber : nationalIdNumber.Trim();
        string? effectiveBirthCertificateNumber = string.IsNullOrWhiteSpace(birthCertificateNumber)
            ? BirthCertificateNumber : birthCertificateNumber.Trim();
        EnsureChildHasIdentity(relationshipToPrimary, effectiveNationalIdNumber, effectiveBirthCertificateNumber);

        FullName = fullName.Trim();
        RelationshipToPrimary = relationshipToPrimary.Trim();
        DateOfBirth = dateOfBirth;
        Phone = BangladeshMobileNumber.Normalize(phone);
        NationalIdNumber = effectiveNationalIdNumber;
        Gender = gender?.Trim();
        BirthCertificateNumber = effectiveBirthCertificateNumber;
        BloodGroup = bloodGroup?.Trim();
        Religion = religion?.Trim();
        Nationality = nationality?.Trim();
        Occupation = occupation?.Trim();
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>Removes this member from the active household without moving out the whole
    /// registration — e.g. a family member relocating independently.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    public void SetPrimaryPhoto(Guid? photoAttachmentId)
    {
        PrimaryPhotoAttachmentId = photoAttachmentId;
    }
}
