using System.Text.RegularExpressions;
using FluentValidation;
using MyCondo.Application.Common.Validation;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;

public sealed partial class ProvisionOrganizationWithAdminCommandValidator
    : AbstractValidator<ProvisionOrganizationWithAdminCommand>
{
    public ProvisionOrganizationWithAdminCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(30)
            .Matches(CodePattern())
            .WithMessage("Code must be uppercase alphanumeric with single hyphens between segments (e.g. 'ARP').");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(63)
            .Matches(SlugPattern())
            .WithMessage("Slug must be lowercase alphanumeric with single hyphens between segments (e.g. 'arp-flat-owners').");

        RuleFor(x => x.AdministratorFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdministratorEmail).NotEmpty().EmailAddress().MaximumLength(320);

        RuleFor(x => x.AdministratorPassword).NotEmpty().MustBeAStrongPassword();

        RuleFor(x => x.EnabledModuleKeys)
            .Must(keys => keys.All(TenantModuleKeys.IsKnown))
            .WithMessage($"Module keys must be one of: {string.Join(", ", TenantModuleKeys.All)}.");
    }

    [GeneratedRegex("^[a-z0-9](-?[a-z0-9])*$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex("^[A-Z0-9](-?[A-Z0-9])*$")]
    private static partial Regex CodePattern();
}
