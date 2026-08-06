using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.EndRatePlanPeriod;

public sealed class EndRatePlanPeriodCommandValidator : AbstractValidator<EndRatePlanPeriodCommand>
{
    public EndRatePlanPeriodCommandValidator()
    {
        RuleFor(x => x.RatePlanId).NotEmpty();
        RuleFor(x => x.EffectiveTo).NotEmpty();
    }
}
