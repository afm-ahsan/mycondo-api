using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.EndTenantFeatureOverride;

public sealed class EndTenantFeatureOverrideCommandValidator : AbstractValidator<EndTenantFeatureOverrideCommand>
{
    public EndTenantFeatureOverrideCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.OverrideId).NotEmpty();
    }
}
