using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.Common;

namespace MyCondo.Domain.Features.Security.DomesticWorkers;

/// <summary>
/// The shared party record for a domestic worker (maid, cook, driver, etc.) — assigned to one or more
/// flats via <see cref="DomesticWorkerAssignments.DomesticWorkerAssignment"/>. "Expired" is not a
/// stored status here; it's derived from an assignment's validity window at check-in time, avoiding
/// a status field that could silently go stale.
/// </summary>
public sealed class DomesticWorkerProfile : AggregateRoot<DomesticWorkerProfileId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string FullName { get; private set; }
    public string Phone { get; private set; }
    public DomesticWorkerType WorkerType { get; private set; }
    public string? IdentityDocumentType { get; private set; }
    public string? IdentityDocumentNumber { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public VerificationStatus VerificationStatus { get; private set; }
    public RecurringAccessProfileStatus Status { get; private set; }
    public string? StatusReason { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }

    private DomesticWorkerProfile()
    {
        FullName = null!;
        Phone = null!;
    }

    private DomesticWorkerProfile(
        DomesticWorkerProfileId id,
        Guid tenantId,
        string fullName,
        string phone,
        DomesticWorkerType workerType,
        string? identityDocumentType,
        string? identityDocumentNumber,
        string? emergencyContactName,
        string? emergencyContactPhone,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FullName = fullName;
        Phone = phone;
        WorkerType = workerType;
        IdentityDocumentType = identityDocumentType;
        IdentityDocumentNumber = identityDocumentNumber;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        VerificationStatus = VerificationStatus.Unverified;
        Status = RecurringAccessProfileStatus.Active;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static DomesticWorkerProfile Register(
        Guid tenantId,
        string fullName,
        string phone,
        DomesticWorkerType workerType,
        string? identityDocumentType,
        string? identityDocumentNumber,
        string? emergencyContactName,
        string? emergencyContactPhone,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new DomesticWorkerProfile(
            DomesticWorkerProfileId.New(), tenantId, fullName.Trim(), phone.Trim(), workerType,
            identityDocumentType?.Trim(), identityDocumentNumber?.Trim(), emergencyContactName?.Trim(),
            emergencyContactPhone?.Trim(), nowUtc);
    }

    public void Verify()
    {
        VerificationStatus = VerificationStatus.Verified;
        Version++;
    }

    public void Reject()
    {
        VerificationStatus = VerificationStatus.Rejected;
        Version++;
    }

    public void Suspend(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = RecurringAccessProfileStatus.Suspended;
        StatusReason = reason.Trim();
        Version++;
    }

    public void Block(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = RecurringAccessProfileStatus.Blocked;
        StatusReason = reason.Trim();
        Version++;
    }

    public void Reactivate()
    {
        Status = RecurringAccessProfileStatus.Active;
        StatusReason = null;
        Version++;
    }

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
