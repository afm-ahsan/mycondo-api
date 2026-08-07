using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.ApproveBooking;

public sealed class ApproveBookingCommandValidator : AbstractValidator<ApproveBookingCommand>
{
    public ApproveBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
    }
}
