namespace MyCondo.Application.Features.Security.Parcels.DTOs;

public sealed record ParcelDto(
    Guid ParcelId,
    string? ParcelReference,
    string? CourierProvider,
    string? TrackingNumber,
    string? SenderName,
    Guid RecipientFlatId,
    Guid? RecipientResidentId,
    string ParcelType,
    int PackageCount,
    DateTimeOffset ReceivedAtUtc,
    Guid? ReceivedBy,
    string? StorageLocation,
    string NotificationStatus,
    string Status,
    DateTimeOffset? CollectedAtUtc,
    Guid? CollectedBy,
    string? CollectorName,
    string? CollectionAcknowledgement,
    string? DamageNote,
    string? CloseReason);
