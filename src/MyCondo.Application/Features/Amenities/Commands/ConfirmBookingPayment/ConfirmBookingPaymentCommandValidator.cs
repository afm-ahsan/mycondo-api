using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.ConfirmBookingPayment;

public sealed class ConfirmBookingPaymentCommandValidator : AbstractValidator<ConfirmBookingPaymentCommand>
{
    public ConfirmBookingPaymentCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
    }
}
