using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.UpdateTenantFeatureOverride;

public sealed class UpdateTenantFeatureOverrideCommandValidator : AbstractValidator<UpdateTenantFeatureOverrideCommand>
{
    public UpdateTenantFeatureOverrideCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.OverrideId).NotEmpty();
        RuleFor(x => x.Reason)
            .Must(reason => reason is null || reason.Trim().Length > 0)
            .WithMessage("Reason cannot be whitespace-only.");
        RuleFor(x => x.EffectiveUntil)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveUntil is not null)
            .WithMessage("EffectiveUntil must not be before EffectiveFrom.");
    }
}
