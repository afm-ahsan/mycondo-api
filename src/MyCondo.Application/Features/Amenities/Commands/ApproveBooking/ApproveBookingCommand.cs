using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.ApproveBooking;

public sealed record ApproveBookingCommand(Guid BookingId) : IRequest<BookingDto>;
