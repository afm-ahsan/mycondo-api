using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Common;

/// <summary>Resolves the commercial FeatureKey for booking-lifecycle requests that carry only
/// <c>BookingId</c> (ADR-033 Task 09A §14) by following Booking → FacilityId → FacilityType with two thin
/// tenant-scoped projections (no full aggregate load). One dedicated implementation shared by the whole
/// "BookingId-only" request family via the generic constraint.</summary>
public sealed class BookingFacilityFeatureResolver<TRequest>(
    IBookingRepository bookings,
    IFacilityRepository facilities
) : IRequestFeatureResolver<TRequest>
    where TRequest : IHasBookingId
{
    public async Task<string> ResolveFeatureKeyAsync(TRequest request, Guid tenantId, CancellationToken cancellationToken)
    {
        BookingId bookingId = new(request.BookingId);
        FacilityId facilityId = await bookings.GetFacilityIdAsync(tenantId, bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        FacilityType facilityType = await facilities.GetFacilityTypeAsync(tenantId, facilityId, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), facilityId.Value);

        return FacilityFeatureKeyMapper.ToFeatureKey(facilityType);
    }
}
