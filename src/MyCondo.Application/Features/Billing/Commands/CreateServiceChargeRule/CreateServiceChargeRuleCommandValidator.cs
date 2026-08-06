using FluentValidation;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Billing.Commands.CreateServiceChargeRule;

public sealed class CreateServiceChargeRuleCommandValidator : AbstractValidator<CreateServiceChargeRuleCommand>
{
    public CreateServiceChargeRuleCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Rate).GreaterThan(0);
        RuleFor(x => x.EffectiveFrom).NotEmpty();

        RuleFor(x => x.CalculationMethod).Must(BeAValidCalculationMethod)
            .WithMessage($"CalculationMethod must be one of: {string.Join(", ", Enum.GetNames<CalculationMethod>())}.");

        RuleFor(x => x.Frequency).Must(BeAValidFrequency)
            .WithMessage($"Frequency must be one of: {string.Join(", ", Enum.GetNames<BillingFrequency>())}.");

        RuleFor(x => x.UnitTypeFilter).Must(BeAValidFlatType!).When(x => x.UnitTypeFilter is not null)
            .WithMessage($"UnitTypeFilter must be one of: {string.Join(", ", Enum.GetNames<FlatType>())}.");
    }

    private static bool BeAValidCalculationMethod(string value) => Enum.TryParse<CalculationMethod>(value, out _);

    private static bool BeAValidFrequency(string value) => Enum.TryParse<BillingFrequency>(value, out _);

    private static bool BeAValidFlatType(string value) => Enum.TryParse<FlatType>(value, out _);
}
