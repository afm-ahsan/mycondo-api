using FluentValidation;

namespace MyCondo.Application.Features.Property.Flats.Commands.DeactivateFlat;

public sealed class DeactivateFlatCommandValidator : AbstractValidator<DeactivateFlatCommand>
{
    public DeactivateFlatCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
    }
}
