namespace MyCondo.Domain.Features.Amenities.Bookings;

/// <summary>
/// Query-time filter value only — mirrors mycondo-web's `derivePaymentStatus()` (booking payment
/// status is derived from <see cref="Booking.PaymentRequired"/>/<see cref="Booking.InvoiceId"/>/
/// <see cref="Booking.DepositCollectionPostingId"/>, not a persisted column). Deliberately a distinct
/// type from <c>MyCondo.Domain.Features.Payments.Payments.PaymentStatus</c> (actual resident payment
/// records), not a redundant duplicate — the two concepts have different domains and lifecycles.
/// </summary>
public enum BookingPaymentStatus
{
    NotRequired = 0,
    AwaitingPayment = 1,
    Paid = 2,
}
