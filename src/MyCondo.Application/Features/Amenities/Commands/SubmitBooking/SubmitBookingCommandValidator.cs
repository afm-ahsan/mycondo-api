using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.SubmitBooking;

public sealed class SubmitBookingCommandValidator : AbstractValidator<SubmitBookingCommand>
{
    public SubmitBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
    }
}
