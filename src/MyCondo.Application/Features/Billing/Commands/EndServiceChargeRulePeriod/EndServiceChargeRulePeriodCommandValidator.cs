using FluentValidation;

namespace MyCondo.Application.Features.Billing.Commands.EndServiceChargeRulePeriod;

public sealed class EndServiceChargeRulePeriodCommandValidator : AbstractValidator<EndServiceChargeRulePeriodCommand>
{
    public EndServiceChargeRulePeriodCommandValidator()
    {
        RuleFor(x => x.ServiceChargeRuleId).NotEmpty();
        RuleFor(x => x.EffectiveTo).NotEmpty();
    }
}
