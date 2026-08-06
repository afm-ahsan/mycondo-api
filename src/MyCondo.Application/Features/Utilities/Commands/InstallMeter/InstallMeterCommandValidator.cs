using FluentValidation;
using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Application.Features.Utilities.Commands.InstallMeter;

public sealed class InstallMeterCommandValidator : AbstractValidator<InstallMeterCommand>
{
    public InstallMeterCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.MeterNumber).NotEmpty().MaximumLength(60);
        RuleFor(x => x.UtilityType).Must(BeAValidUtilityType)
            .WithMessage($"UtilityType must be one of: {string.Join(", ", Enum.GetNames<UtilityType>())}.");
    }

    private static bool BeAValidUtilityType(string value) => Enum.TryParse<UtilityType>(value, out _);
}
