using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Queries.GetBookingById;

public sealed record GetBookingByIdQuery(Guid BookingId) : IRequest<BookingDto>;
