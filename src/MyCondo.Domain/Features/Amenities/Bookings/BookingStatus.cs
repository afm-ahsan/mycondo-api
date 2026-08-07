namespace MyCondo.Domain.Features.Amenities.Bookings;

public enum BookingStatus
{
    Draft = 0,
    PendingApproval = 1,
    AwaitingPayment = 2,
    Confirmed = 3,
    CheckedIn = 4,
    Completed = 5,
    Cancelled = 6,
    Rejected = 7,
    NoShow = 8,
    ClosedAfterInspection = 9,
}
