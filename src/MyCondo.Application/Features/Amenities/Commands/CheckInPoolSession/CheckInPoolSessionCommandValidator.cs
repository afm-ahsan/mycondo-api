using FluentValidation;
using MyCondo.Domain.Features.Amenities.PoolSessions;

namespace MyCondo.Application.Features.Amenities.Commands.CheckInPoolSession;

public sealed class CheckInPoolSessionCommandValidator : AbstractValidator<CheckInPoolSessionCommand>
{
    public CheckInPoolSessionCommandValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.PersonType).Must(BeAValidPersonType)
            .WithMessage($"PersonType must be one of: {string.Join(", ", Enum.GetNames<PoolPersonType>())}.");
        RuleFor(x => x.AgeCategory).Must(BeAValidAgeCategory)
            .WithMessage($"AgeCategory must be one of: {string.Join(", ", Enum.GetNames<PoolAgeCategory>())}.");
        RuleFor(x => x.OverrideReason).MaximumLength(500);
    }

    private static bool BeAValidPersonType(string value) => Enum.TryParse<PoolPersonType>(value, out _);

    private static bool BeAValidAgeCategory(string value) => Enum.TryParse<PoolAgeCategory>(value, out _);
}
