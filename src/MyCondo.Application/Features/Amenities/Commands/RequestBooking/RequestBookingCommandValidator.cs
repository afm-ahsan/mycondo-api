using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.RequestBooking;

public sealed class RequestBookingCommandValidator : AbstractValidator<RequestBookingCommand>
{
    public RequestBookingCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(120);
        RuleFor(x => x.EndAtUtc).GreaterThan(x => x.StartAtUtc)
            .WithMessage("EndAtUtc must be after StartAtUtc.");
        RuleFor(x => x.SetupBufferMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CleanupBufferMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExpectedGuestCount).GreaterThan(0);
    }
}
