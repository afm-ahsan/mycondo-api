using MyCondo.Application.Features.Security.Parcels.DTOs;
using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Application.Features.Security.Parcels.Mappings;

internal static class ParcelMappings
{
    public static ParcelDto ToDto(this Parcel parcel) => new(
        parcel.Id.Value, parcel.ParcelReference, parcel.CourierProvider, parcel.TrackingNumber,
        parcel.SenderName, parcel.RecipientFlatId.Value, parcel.RecipientResidentId?.Value,
        parcel.ParcelType.ToString(), parcel.PackageCount, parcel.ReceivedAtUtc, parcel.ReceivedBy,
        parcel.StorageLocation, parcel.NotificationStatus.ToString(), parcel.Status.ToString(),
        parcel.CollectedAtUtc, parcel.CollectedBy, parcel.CollectorName, parcel.CollectionAcknowledgement,
        parcel.DamageNote, parcel.CloseReason);
}
