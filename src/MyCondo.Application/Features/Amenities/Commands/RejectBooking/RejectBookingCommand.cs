using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.RejectBooking;

public sealed record RejectBookingCommand(Guid BookingId, string Reason) : IRequest<BookingDto>;
