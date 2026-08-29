using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.ReactivateOrganizationSubscription;

public sealed class ReactivateOrganizationSubscriptionCommandValidator : AbstractValidator<ReactivateOrganizationSubscriptionCommand>
{
    public ReactivateOrganizationSubscriptionCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
