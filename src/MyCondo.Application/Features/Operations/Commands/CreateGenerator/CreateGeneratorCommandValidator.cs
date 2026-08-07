using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.CreateGenerator;

public sealed class CreateGeneratorCommandValidator : AbstractValidator<CreateGeneratorCommand>
{
    public CreateGeneratorCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Model).MaximumLength(120);
        RuleFor(x => x.CapacityKva).GreaterThanOrEqualTo(0).When(x => x.CapacityKva is not null);
        RuleFor(x => x.Location).MaximumLength(200);
    }
}
