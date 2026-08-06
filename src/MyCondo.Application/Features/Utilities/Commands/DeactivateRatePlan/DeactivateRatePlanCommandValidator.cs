using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.DeactivateRatePlan;

public sealed class DeactivateRatePlanCommandValidator : AbstractValidator<DeactivateRatePlanCommand>
{
    public DeactivateRatePlanCommandValidator()
    {
        RuleFor(x => x.RatePlanId).NotEmpty();
    }
}
