using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Commands.DeactivateBlackoutDate;

public sealed class DeactivateBlackoutDateCommandValidator : AbstractValidator<DeactivateBlackoutDateCommand>
{
    public DeactivateBlackoutDateCommandValidator()
    {
        RuleFor(x => x.BlackoutDateId).NotEmpty();
    }
}
