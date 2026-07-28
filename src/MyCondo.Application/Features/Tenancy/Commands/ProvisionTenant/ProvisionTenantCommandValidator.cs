using System.Text.RegularExpressions;
using FluentValidation;

namespace MyCondo.Application.Features.Tenancy.Commands.ProvisionTenant;

public sealed partial class ProvisionTenantCommandValidator : AbstractValidator<ProvisionTenantCommand>
{
    public ProvisionTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(63)
            .Matches(SlugPattern())
            .WithMessage("Slug must be lowercase alphanumeric with single hyphens between segments (e.g. 'arp-flat-owners').");
    }

    [GeneratedRegex("^[a-z0-9](-?[a-z0-9])*$")]
    private static partial Regex SlugPattern();
}
