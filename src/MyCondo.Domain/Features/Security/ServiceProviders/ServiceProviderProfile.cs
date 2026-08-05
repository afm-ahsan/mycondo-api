using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.Common;

namespace MyCondo.Domain.Features.Security.ServiceProviders;

/// <summary>
/// The shared party record for a recurring service provider (teacher, tutor, nurse, therapist,
/// trainer, etc.) — mirrors <see cref="DomesticWorkers.DomesticWorkerProfile"/> structurally per the
/// register digitization spec's own instruction that business rules "mirror domestic staff where
/// applicable."
/// </summary>
public sealed class ServiceProviderProfile : AggregateRoot<ServiceProviderProfileId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string FullName { get; private set; }
    public string Phone { get; private set; }
    public ServiceProviderType ProviderType { get; private set; }
    public string? ServiceDescription { get; private set; }
    public string? IdentityDocumentType { get; private set; }
    public string? IdentityDocumentNumber { get; private set; }
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

    private ServiceProviderProfile()
    {
        FullName = null!;
        Phone = null!;
    }

    private ServiceProviderProfile(
        ServiceProviderProfileId id,
        Guid tenantId,
        string fullName,
        string phone,
        ServiceProviderType providerType,
        string? serviceDescription,
        string? identityDocumentType,
        string? identityDocumentNumber,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        FullName = fullName;
        Phone = phone;
        ProviderType = providerType;
        ServiceDescription = serviceDescription;
        IdentityDocumentType = identityDocumentType;
        IdentityDocumentNumber = identityDocumentNumber;
        VerificationStatus = VerificationStatus.Unverified;
        Status = RecurringAccessProfileStatus.Active;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static ServiceProviderProfile Register(
        Guid tenantId,
        string fullName,
        string phone,
        ServiceProviderType providerType,
        string? serviceDescription,
        string? identityDocumentType,
        string? identityDocumentNumber,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new ServiceProviderProfile(
            ServiceProviderProfileId.New(), tenantId, fullName.Trim(), phone.Trim(), providerType,
            serviceDescription?.Trim(), identityDocumentType?.Trim(), identityDocumentNumber?.Trim(), nowUtc);
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
