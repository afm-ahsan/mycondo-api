using FluentValidation;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.RatePlans;

namespace MyCondo.Application.Features.Utilities.Commands.CreateRatePlan;

public sealed class CreateRatePlanCommandValidator : AbstractValidator<CreateRatePlanCommand>
{
    public CreateRatePlanCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FixedServiceCharge).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxPercentage).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EffectiveFrom).NotEmpty();

        RuleFor(x => x.UtilityType).Must(BeAValidUtilityType)
            .WithMessage($"UtilityType must be one of: {string.Join(", ", Enum.GetNames<UtilityType>())}.");

        RuleFor(x => x.Structure).Must(BeAValidStructure)
            .WithMessage($"Structure must be one of: {string.Join(", ", Enum.GetNames<RateStructure>())}.");

        RuleFor(x => x.Slabs).NotNull();
    }

    private static bool BeAValidUtilityType(string value) => Enum.TryParse<UtilityType>(value, out _);

    private static bool BeAValidStructure(string value) => Enum.TryParse<RateStructure>(value, out _);
}
