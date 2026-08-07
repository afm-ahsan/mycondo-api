using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.CompleteBooking;

public sealed class CompleteBookingCommandValidator : AbstractValidator<CompleteBookingCommand>
{
    public CompleteBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
    }
}
