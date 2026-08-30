using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.ChangeOrganizationSubscription;

public sealed class ChangeOrganizationSubscriptionCommandValidator : AbstractValidator<ChangeOrganizationSubscriptionCommand>
{
    public ChangeOrganizationSubscriptionCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.SubscriptionPackageVersionId).NotEmpty();
        RuleFor(x => x.BillingCycle).IsInEnum();
    }
}
