using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.GetUpcomingBookings;

public sealed record GetUpcomingBookingsQuery : IRequest<IReadOnlyList<BookingDto>>;
