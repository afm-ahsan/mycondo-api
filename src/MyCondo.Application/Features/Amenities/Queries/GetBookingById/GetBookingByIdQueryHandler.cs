using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Features.Amenities.Bookings;

namespace MyCondo.Application.Features.Amenities.Queries.GetBookingById;

public sealed class GetBookingByIdQueryHandler(
    IBookingRepository bookings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetBookingByIdQuery, BookingDto>
{
    public async ValueTask<BookingDto> Handle(GetBookingByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BookingId id = new(query.BookingId);
        Booking booking = await bookings.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), query.BookingId);
        if (booking.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Booking), query.BookingId);
        }

        return booking.ToDto();
    }
}
