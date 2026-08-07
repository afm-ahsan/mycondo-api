using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.CheckInBooking;

public sealed record CheckInBookingCommand(Guid BookingId) : IRequest<BookingDto>;
