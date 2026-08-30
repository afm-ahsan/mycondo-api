using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.MarkOrganizationSubscriptionPastDue;

public sealed class MarkOrganizationSubscriptionPastDueCommandValidator : AbstractValidator<MarkOrganizationSubscriptionPastDueCommand>
{
    public MarkOrganizationSubscriptionPastDueCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
