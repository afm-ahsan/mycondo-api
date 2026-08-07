using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Amenities.Bookings.Exceptions;

public sealed class BookingInvalidTransitionException(BookingId bookingId, BookingStatus currentStatus, string attemptedAction)
    : DomainException($"Booking {bookingId} in status {currentStatus} cannot {attemptedAction}.")
{
    public BookingId BookingId { get; } = bookingId;
    public BookingStatus CurrentStatus { get; } = currentStatus;
}
