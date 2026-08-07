using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.InspectBooking;

public sealed record InspectBookingCommand(
    Guid BookingId,
    string? Notes,
    decimal? DamageDeductionAmount,
    string? DamageDeductionReason
) : IRequest<BookingDto>;
