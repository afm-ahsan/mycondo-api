using Mediator;
using MyCondo.Application.Features.Amenities.DTOs;

namespace MyCondo.Application.Features.Amenities.Commands.ConfirmBookingPayment;

public sealed record ConfirmBookingPaymentCommand(Guid BookingId) : IRequest<BookingDto>;
