using FluentValidation;

namespace MyCondo.Application.Features.Property.Gates.Commands.CreateGate;

public sealed class CreateGateCommandValidator : AbstractValidator<CreateGateCommand>
{
    public CreateGateCommandValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
