using FluentValidation;

namespace MyCondo.Application.Features.Property.Flats.Commands.UpdateFlatArea;

public sealed class UpdateFlatAreaCommandValidator : AbstractValidator<UpdateFlatAreaCommand>
{
    public UpdateFlatAreaCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.AreaSqFt).GreaterThan(0).When(x => x.AreaSqFt is not null);
    }
}
