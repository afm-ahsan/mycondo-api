using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Security.Parcels.Exceptions;

public sealed class ParcelInvalidStatusTransitionException(ParcelId parcelId, ParcelStatus from, ParcelStatus to)
    : DomainException($"Parcel {parcelId} cannot transition from {from} to {to}.")
{
    public ParcelId ParcelId { get; } = parcelId;
    public ParcelStatus From { get; } = from;
    public ParcelStatus To { get; } = to;
}
