using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.RestrictOrganizationSubscription;

public sealed class RestrictOrganizationSubscriptionCommandValidator : AbstractValidator<RestrictOrganizationSubscriptionCommand>
{
    public RestrictOrganizationSubscriptionCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
