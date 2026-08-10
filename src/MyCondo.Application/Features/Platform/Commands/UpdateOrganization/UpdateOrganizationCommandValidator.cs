using System.Text.RegularExpressions;
using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.UpdateOrganization;

public sealed partial class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Code)
            .MaximumLength(30)
            .Matches(CodePattern())
            .WithMessage("Code must be uppercase alphanumeric with single hyphens between segments (e.g. 'ARP').")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));
    }

    [GeneratedRegex("^[A-Z0-9](-?[A-Z0-9])*$")]
    private static partial Regex CodePattern();
}
