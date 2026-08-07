using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.CheckInBooking;

public sealed class CheckInBookingCommandValidator : AbstractValidator<CheckInBookingCommand>
{
    public CheckInBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
    }
}
