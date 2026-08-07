using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.CompleteBooking;

public sealed record CompleteBookingCommand(Guid BookingId) : IRequest<BookingDto>;
