using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.ExpireOrganizationSubscription;

public sealed class ExpireOrganizationSubscriptionCommandValidator : AbstractValidator<ExpireOrganizationSubscriptionCommand>
{
    public ExpireOrganizationSubscriptionCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
