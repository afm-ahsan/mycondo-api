using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.SubmitBooking;

public sealed record SubmitBookingCommand(Guid BookingId) : IRequest<BookingDto>;
