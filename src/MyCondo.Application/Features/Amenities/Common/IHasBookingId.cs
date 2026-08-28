namespace MyCondo.Application.Features.Amenities.Common;

/// <summary>Implemented by booking-lifecycle requests that carry only <c>BookingId</c> — their commercial
/// feature (Community Hall vs. Swimming Pool) cannot be read off the request and must be resolved via
/// <see cref="BookingFacilityFeatureResolver{TRequest}"/> (ADR-033 Task 09A).</summary>
public interface IHasBookingId
{
    Guid BookingId { get; }
}
