using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations.Exceptions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

/// <summary>
/// The structured "Tenant Registration" application/verification/approval workflow for a person taking
/// up occupancy of a <see cref="Flat"/> — named <c>OccupancyRegistration</c> rather than
/// <c>TenantRegistration</c> to avoid colliding with this codebase's unrelated multi-tenancy vocabulary
/// (<see cref="ITenantScoped.TenantId"/>, <c>tenant.manage</c>); "Tenant Registration" remains the
/// human-facing name in UI copy and permission descriptions. Links to a shared <see cref="Resident"/>
/// party record for name/phone/email reuse across registers, but captures its own sensitive,
/// registration-specific fields (NID, date of birth, permanent address, photo) here rather than on
/// <see cref="Resident"/>, since those are specific to this occupancy event and require tighter,
/// separately-maskable access control. Two-stage approval (owner, then management) mirrors the paper
/// register's real-world sign-off chain; either stage can send the registration back for correction or
/// reject it outright, but not both at once — see <see cref="OccupancyRegistrationStatus"/>.
/// </summary>
public sealed class OccupancyRegistration : AggregateRoot<OccupancyRegistrationId>, IAuditable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public FlatId FlatId { get; private set; }
    public ResidentId PrimaryResidentId { get; private set; }
    public ResidentType OccupancyType { get; private set; }
    public string PrimaryFullName { get; private set; }
    public string? PrimaryPhone { get; private set; }
    public string? PrimaryEmail { get; private set; }
    public string? PrimaryNationalIdNumber { get; private set; }
    public DateOnly? PrimaryDateOfBirth { get; private set; }
    public string? PrimaryPermanentAddress { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public Guid? PrimaryPhotoAttachmentId { get; private set; }
    public DateOnly? MoveInExpectedDate { get; private set; }
    public OccupancyRegistrationStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public Guid? SubmittedBy { get; private set; }
    public DateTimeOffset? OwnerReviewedAtUtc { get; private set; }
    public Guid? OwnerReviewedBy { get; private set; }
    public DateTimeOffset? ManagementVerifiedAtUtc { get; private set; }
    public Guid? ManagementVerifiedBy { get; private set; }
    public DateTimeOffset? ActivatedAtUtc { get; private set; }
    public DateTimeOffset? MovedOutAtUtc { get; private set; }
    public string? MoveOutReason { get; private set; }
    public string? CorrectionsRequestedReason { get; private set; }
    public string? RejectedReason { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private OccupancyRegistration()
    {
        PrimaryFullName = null!;
    }

    private OccupancyRegistration(
        OccupancyRegistrationId id, Guid tenantId, FlatId flatId, ResidentId primaryResidentId,
        ResidentType occupancyType, string primaryFullName, string? primaryPhone, string? primaryEmail,
        string? primaryNationalIdNumber, DateOnly? primaryDateOfBirth, string? primaryPermanentAddress,
        string? emergencyContactName, string? emergencyContactPhone, DateOnly? moveInExpectedDate,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FlatId = flatId;
        PrimaryResidentId = primaryResidentId;
        OccupancyType = occupancyType;
        PrimaryFullName = primaryFullName;
        PrimaryPhone = primaryPhone;
        PrimaryEmail = primaryEmail;
        PrimaryNationalIdNumber = primaryNationalIdNumber;
        PrimaryDateOfBirth = primaryDateOfBirth;
        PrimaryPermanentAddress = primaryPermanentAddress;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        MoveInExpectedDate = moveInExpectedDate;
        Status = OccupancyRegistrationStatus.Draft;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static OccupancyRegistration Register(
        Guid tenantId, FlatId flatId, ResidentId primaryResidentId, ResidentType occupancyType,
        string primaryFullName, string? primaryPhone, string? primaryEmail, string? primaryNationalIdNumber,
        DateOnly? primaryDateOfBirth, string? primaryPermanentAddress, string? emergencyContactName,
        string? emergencyContactPhone, DateOnly? moveInExpectedDate, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryFullName);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new OccupancyRegistration(
            OccupancyRegistrationId.New(), tenantId, flatId, primaryResidentId, occupancyType,
            primaryFullName.Trim(), primaryPhone?.Trim(), primaryEmail?.Trim(), primaryNationalIdNumber?.Trim(),
            primaryDateOfBirth, primaryPermanentAddress?.Trim(), emergencyContactName?.Trim(),
            emergencyContactPhone?.Trim(), moveInExpectedDate, nowUtc);
    }

    private void EnsureEditable(string attemptedAction)
    {
        if (Status is not (OccupancyRegistrationStatus.Draft or OccupancyRegistrationStatus.CorrectionsRequested))
        {
            throw new OccupancyRegistrationInvalidTransitionException(Id, Status, attemptedAction);
        }
    }

    public void UpdateDraft(
        string primaryFullName, string? primaryPhone, string? primaryEmail, string? primaryNationalIdNumber,
        DateOnly? primaryDateOfBirth, string? primaryPermanentAddress, string? emergencyContactName,
        string? emergencyContactPhone, DateOnly? moveInExpectedDate)
    {
        EnsureEditable("be edited");
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryFullName);

        PrimaryFullName = primaryFullName.Trim();
        PrimaryPhone = primaryPhone?.Trim();
        PrimaryEmail = primaryEmail?.Trim();
        // The National ID is masked on every read, so the client can never round-trip the existing
        // value back through this form — an empty submission means "not retyped", not "clear it".
        // Replacing it requires a new full value; see IdentityMasking / the security review.
        if (!string.IsNullOrWhiteSpace(primaryNationalIdNumber))
        {
            PrimaryNationalIdNumber = primaryNationalIdNumber.Trim();
        }
        PrimaryDateOfBirth = primaryDateOfBirth;
        PrimaryPermanentAddress = primaryPermanentAddress?.Trim();
        EmergencyContactName = emergencyContactName?.Trim();
        EmergencyContactPhone = emergencyContactPhone?.Trim();
        MoveInExpectedDate = moveInExpectedDate;
        Version++;
    }

    public void SetPrimaryPhoto(Guid? photoAttachmentId)
    {
        EnsureEditable("have its photo updated");
        PrimaryPhotoAttachmentId = photoAttachmentId;
        Version++;
    }

    public void Submit(Guid? submittedBy, DateTimeOffset nowUtc)
    {
        EnsureEditable("be submitted");

        Status = OccupancyRegistrationStatus.Submitted;
        SubmittedAtUtc = nowUtc;
        SubmittedBy = submittedBy;
        CorrectionsRequestedReason = null;
        Version++;
    }

    public void ApproveByOwner(Guid? approvedBy, DateTimeOffset nowUtc)
    {
        if (Status != OccupancyRegistrationStatus.Submitted)
        {
            throw new OccupancyRegistrationInvalidTransitionException(Id, Status, "be approved by the owner");
        }

        Status = OccupancyRegistrationStatus.OwnerApproved;
        OwnerReviewedAtUtc = nowUtc;
        OwnerReviewedBy = approvedBy;
        Version++;
    }

    public void VerifyByManagement(Guid? verifiedBy, DateTimeOffset nowUtc)
    {
        if (Status != OccupancyRegistrationStatus.OwnerApproved)
        {
            throw new OccupancyRegistrationInvalidTransitionException(Id, Status, "be verified by management");
        }

        Status = OccupancyRegistrationStatus.ManagementVerified;
        ManagementVerifiedAtUtc = nowUtc;
        ManagementVerifiedBy = verifiedBy;
        Version++;
    }

    public void RequestCorrections(string reason, DateTimeOffset nowUtc)
    {
        if (Status is not (OccupancyRegistrationStatus.Submitted or OccupancyRegistrationStatus.OwnerApproved))
        {
            throw new OccupancyRegistrationInvalidTransitionException(Id, Status, "have corrections requested");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = OccupancyRegistrationStatus.CorrectionsRequested;
        CorrectionsRequestedReason = reason.Trim();
        Version++;
    }

    public void Reject(string reason, DateTimeOffset nowUtc)
    {
        if (Status is not (OccupancyRegistrationStatus.Submitted or OccupancyRegistrationStatus.OwnerApproved))
        {
            throw new OccupancyRegistrationInvalidTransitionException(Id, Status, "be rejected");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = OccupancyRegistrationStatus.Rejected;
        RejectedReason = reason.Trim();
        Version++;
    }

    /// <summary>Confirms move-in — the registration becomes the flat's active occupancy record.</summary>
    public void Activate(DateTimeOffset nowUtc)
    {
        if (Status != OccupancyRegistrationStatus.ManagementVerified)
        {
            throw new OccupancyRegistrationInvalidTransitionException(Id, Status, "be activated");
        }

        Status = OccupancyRegistrationStatus.Active;
        ActivatedAtUtc = nowUtc;
        Version++;
    }

    /// <summary>Ends the occupancy. Household members must be deactivated separately by the caller
    /// (application-layer cascade) so each deactivation is its own auditable event.</summary>
    public void MoveOut(string? reason, DateTimeOffset nowUtc)
    {
        if (Status != OccupancyRegistrationStatus.Active)
        {
            throw new OccupancyRegistrationInvalidTransitionException(Id, Status, "be moved out");
        }

        Status = OccupancyRegistrationStatus.MovedOut;
        MovedOutAtUtc = nowUtc;
        MoveOutReason = reason?.Trim();
        Version++;
    }
}
