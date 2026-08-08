using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Amenities.Bookings;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(BookingId id, CancellationToken cancellationToken);

    /// <summary><paramref name="fromDate"/>/<paramref name="toDate"/> bound <see cref="Booking.StartAtUtc"/>
    /// as <c>[fromDate, toDate)</c>, same semantics as <see cref="GetForPeriodAsync"/> — filtering happens
    /// in this query, before pagination, never in application code. <paramref name="eventType"/> is a
    /// case-insensitive contains match. <paramref name="paymentStatus"/> filters on the same derived
    /// condition <c>BookingPaymentStatus</c> encodes (never a persisted column).</summary>
    Task<PagedResult<Booking>> SearchAsync(
        Guid tenantId, FacilityId? facilityId, FlatId? flatId, BookingStatus? status, BuildingId? buildingId,
        string? eventType, BookingPaymentStatus? paymentStatus, DateTimeOffset? fromDate, DateTimeOffset? toDate,
        int page, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<Booking>> GetUpcomingAsync(
        Guid tenantId, DateTimeOffset fromUtc, CancellationToken cancellationToken);

    /// <summary>UX-friendly pre-check mirroring the DB's <c>ex_bookings_no_overlap</c> partial
    /// EXCLUDE/GiST constraint (same "app pre-check + DB backstop" pattern as
    /// RatePlan.HasOverlappingPlanAsync) — every status except Cancelled/Rejected/NoShow holds the
    /// slot. <paramref name="effectiveStartUtc"/>/<paramref name="effectiveEndUtc"/> are expected to
    /// already include the requested setup/cleanup buffers.</summary>
    Task<bool> HasOverlappingBookingAsync(
        Guid tenantId, FacilityId facilityId, DateTimeOffset effectiveStartUtc, DateTimeOffset effectiveEndUtc,
        CancellationToken cancellationToken);

    /// <summary>Backs both the facility-utilization and booking-revenue reports — bookings whose
    /// <see cref="Booking.StartAtUtc"/> falls in <c>[fromUtc, toUtc)</c>, optionally narrowed to one
    /// facility.</summary>
    Task<IReadOnlyList<Booking>> GetForPeriodAsync(
        Guid tenantId, DateTimeOffset fromUtc, DateTimeOffset toUtc, FacilityId? facilityId, CancellationToken cancellationToken);

    void Add(Booking booking);
}
