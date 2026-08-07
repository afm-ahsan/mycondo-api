using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.MarkBookingNoShow;

public sealed class MarkBookingNoShowCommandValidator : AbstractValidator<MarkBookingNoShowCommand>
{
    public MarkBookingNoShowCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
    }
}
