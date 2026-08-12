using FluentValidation;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.Flats.Commands.UpdateFlat;

public sealed class UpdateFlatCommandValidator : AbstractValidator<UpdateFlatCommand>
{
    public UpdateFlatCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.FlatNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FloorNumber).GreaterThanOrEqualTo(-5).When(x => x.FloorNumber is not null);
        RuleFor(x => x.FlatType).Must(BeAValidFlatType)
            .WithMessage($"FlatType must be one of: {string.Join(", ", Enum.GetNames<FlatType>())}.");
    }

    private static bool BeAValidFlatType(string value) => Enum.TryParse<FlatType>(value, out _);
}
