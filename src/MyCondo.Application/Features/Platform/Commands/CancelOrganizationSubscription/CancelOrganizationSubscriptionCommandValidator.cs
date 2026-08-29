using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.CancelOrganizationSubscription;

public sealed class CancelOrganizationSubscriptionCommandValidator : AbstractValidator<CancelOrganizationSubscriptionCommand>
{
    public CancelOrganizationSubscriptionCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
