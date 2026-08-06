using FluentValidation;

namespace MyCondo.Application.Features.Billing.Commands.DeactivateServiceChargeRule;

public sealed class DeactivateServiceChargeRuleCommandValidator : AbstractValidator<DeactivateServiceChargeRuleCommand>
{
    public DeactivateServiceChargeRuleCommandValidator()
    {
        RuleFor(x => x.ServiceChargeRuleId).NotEmpty();
    }
}
