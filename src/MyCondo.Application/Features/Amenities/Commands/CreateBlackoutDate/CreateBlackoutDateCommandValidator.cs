using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.CreateBlackoutDate;

public sealed class CreateBlackoutDateCommandValidator : AbstractValidator<CreateBlackoutDateCommand>
{
    public CreateBlackoutDateCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.DateFrom).NotEmpty();
        RuleFor(x => x.DateTo).NotEmpty();
        RuleFor(x => x.DateTo).GreaterThanOrEqualTo(x => x.DateFrom)
            .WithMessage("DateTo must not precede DateFrom.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
