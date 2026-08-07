using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Bookings;

namespace MyCondo.Application.Features.Amenities.Queries.GetUpcomingBookings;

public sealed class GetUpcomingBookingsQueryHandler(
    IBookingRepository bookings,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetUpcomingBookingsQuery, IReadOnlyList<BookingDto>>
{
    public async ValueTask<IReadOnlyList<BookingDto>> Handle(GetUpcomingBookingsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        IReadOnlyList<Booking> items = await bookings.GetUpcomingAsync(tenantId, clock.UtcNow, cancellationToken);

        return items.Select(b => b.ToDto()).ToList();
    }
}
