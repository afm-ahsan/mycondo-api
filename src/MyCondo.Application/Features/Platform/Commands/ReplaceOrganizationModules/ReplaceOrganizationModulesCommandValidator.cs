using FluentValidation;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.ReplaceOrganizationModules;

public sealed class ReplaceOrganizationModulesCommandValidator : AbstractValidator<ReplaceOrganizationModulesCommand>
{
    public ReplaceOrganizationModulesCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();

        RuleFor(x => x.ModuleKeys)
            .Must(keys => keys.All(TenantModuleKeys.IsKnown))
            .WithMessage($"Module keys must be one of: {string.Join(", ", TenantModuleKeys.All)}.");

        RuleFor(x => x.ModuleKeys)
            .Must(keys => keys.Distinct(StringComparer.Ordinal).Count() == keys.Count)
            .WithMessage("Module keys must not contain duplicates.");
    }
}
