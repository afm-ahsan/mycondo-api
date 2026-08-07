using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Amenities.BlackoutDates.Exceptions;

public sealed class BlackoutDateAlreadyInactiveException(BlackoutDateId blackoutDateId)
    : DomainException($"BlackoutDate {blackoutDateId} is already inactive.")
{
    public BlackoutDateId BlackoutDateId { get; } = blackoutDateId;
}
