using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.InspectBooking;

public sealed class InspectBookingCommandValidator : AbstractValidator<InspectBookingCommand>
{
    public InspectBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.DamageDeductionAmount).GreaterThanOrEqualTo(0).When(x => x.DamageDeductionAmount is not null);
        RuleFor(x => x.DamageDeductionReason).MaximumLength(500);
    }
}
