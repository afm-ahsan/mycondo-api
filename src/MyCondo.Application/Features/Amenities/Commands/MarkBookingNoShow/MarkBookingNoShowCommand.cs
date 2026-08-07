using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.MarkBookingNoShow;

public sealed record MarkBookingNoShowCommand(Guid BookingId) : IRequest<BookingDto>;
