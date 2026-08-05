using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Security.Parcels.Exceptions;

namespace MyCondo.Domain.Features.Security.Parcels;

/// <summary>
/// A parcel's custody record from front-desk receipt through handover (or return/rejection/loss).
/// <see cref="ParcelCustodyEvents.ParcelCustodyEvent"/> rows record each transition for the
/// "custody history" the spec requires — written by the command handler alongside each transition
/// here, not raised as a domain event (this codebase's IDomainEventHandler dispatch is for
/// cross-feature side effects, not same-transaction audit rows).
/// </summary>
public sealed class Parcel : AggregateRoot<ParcelId>, IAuditable, ITenantScoped
{
    private static readonly ParcelStatus[] TerminalStatuses =
    [
        ParcelStatus.Collected, ParcelStatus.Returned, ParcelStatus.Rejected, ParcelStatus.Damaged,
        ParcelStatus.LostOrEscalated,
    ];

    public Guid TenantId { get; private set; }
    public string? ParcelReference { get; private set; }
    public string? CourierProvider { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? SenderName { get; private set; }
    public FlatId RecipientFlatId { get; private set; }
    public ResidentId? RecipientResidentId { get; private set; }
    public ParcelType ParcelType { get; private set; }
    public int PackageCount { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public Guid? ReceivedBy { get; private set; }
    public string? StorageLocation { get; private set; }
    public ParcelNotificationStatus NotificationStatus { get; private set; }
    public ParcelStatus Status { get; private set; }
    public DateTimeOffset? CollectedAtUtc { get; private set; }
    public Guid? CollectedBy { get; private set; }
    public string? CollectorName { get; private set; }
    public string? CollectionAcknowledgement { get; private set; }
    public string? DamageNote { get; private set; }
    public string? CloseReason { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Parcel() { }

    private Parcel(
        ParcelId id,
        Guid tenantId,
        string? parcelReference,
        string? courierProvider,
        string? trackingNumber,
        string? senderName,
        FlatId recipientFlatId,
        ResidentId? recipientResidentId,
        ParcelType parcelType,
        int packageCount,
        Guid? receivedBy,
        string? storageLocation,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        ParcelReference = parcelReference;
        CourierProvider = courierProvider;
        TrackingNumber = trackingNumber;
        SenderName = senderName;
        RecipientFlatId = recipientFlatId;
        RecipientResidentId = recipientResidentId;
        ParcelType = parcelType;
        PackageCount = packageCount;
        ReceivedAtUtc = nowUtc;
        ReceivedBy = receivedBy;
        StorageLocation = storageLocation;
        NotificationStatus = ParcelNotificationStatus.NotSent;
        Status = ParcelStatus.Received;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static Parcel Receive(
        Guid tenantId,
        string? parcelReference,
        string? courierProvider,
        string? trackingNumber,
        string? senderName,
        FlatId recipientFlatId,
        ResidentId? recipientResidentId,
        ParcelType parcelType,
        int packageCount,
        Guid? receivedBy,
        string? storageLocation,
        DateTimeOffset nowUtc)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (packageCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(packageCount), "PackageCount must be positive.");
        }

        return new Parcel(
            ParcelId.New(), tenantId, parcelReference?.Trim(), courierProvider?.Trim(), trackingNumber?.Trim(),
            senderName?.Trim(), recipientFlatId, recipientResidentId, parcelType, packageCount, receivedBy,
            storageLocation?.Trim(), nowUtc);
    }

    /// <summary>Received → AwaitingCollection (see <see cref="ParcelStatus.ResidentNotified"/>'s doc
    /// comment for why the intermediate status isn't a separately reachable step today).</summary>
    public void NotifyResident()
    {
        if (Status != ParcelStatus.Received)
        {
            throw new ParcelInvalidStatusTransitionException(Id, Status, ParcelStatus.AwaitingCollection);
        }

        Status = ParcelStatus.AwaitingCollection;
        NotificationStatus = ParcelNotificationStatus.Sent;
        Version++;
    }

    /// <summary>Collection must close the custody record — cannot be collected twice.</summary>
    public void Collect(Guid? collectedBy, string collectorName, string? acknowledgement, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectorName);
        EnsureNotTerminal(ParcelStatus.Collected);

        Status = ParcelStatus.Collected;
        CollectedAtUtc = nowUtc;
        CollectedBy = collectedBy;
        CollectorName = collectorName.Trim();
        CollectionAcknowledgement = acknowledgement?.Trim();
        Version++;
    }

    public void MarkDamaged(string damageNote)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(damageNote);
        EnsureNotTerminal(ParcelStatus.Damaged);

        Status = ParcelStatus.Damaged;
        DamageNote = damageNote.Trim();
        Version++;
    }

    /// <summary>Terminal close for the three non-handover outcomes — Returned/Rejected/LostOrEscalated
    /// share the same "cannot happen twice, reason required" shape.</summary>
    public void Close(ParcelStatus outcome, string reason)
    {
        if (outcome is not (ParcelStatus.Returned or ParcelStatus.Rejected or ParcelStatus.LostOrEscalated))
        {
            throw new ArgumentException(
                $"{outcome} is not a valid Close outcome (must be Returned, Rejected, or LostOrEscalated).",
                nameof(outcome));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        EnsureNotTerminal(outcome);

        Status = outcome;
        CloseReason = reason.Trim();
        Version++;
    }

    private void EnsureNotTerminal(ParcelStatus target)
    {
        if (TerminalStatuses.Contains(Status))
        {
            throw new ParcelInvalidStatusTransitionException(Id, Status, target);
        }
    }
}
