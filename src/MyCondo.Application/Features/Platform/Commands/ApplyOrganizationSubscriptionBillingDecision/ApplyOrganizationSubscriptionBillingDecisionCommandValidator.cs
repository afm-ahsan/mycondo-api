using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.ApplyOrganizationSubscriptionBillingDecision;

public sealed class ApplyOrganizationSubscriptionBillingDecisionCommandValidator
    : AbstractValidator<ApplyOrganizationSubscriptionBillingDecisionCommand>
{
    public ApplyOrganizationSubscriptionBillingDecisionCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
