using FluentValidation;

namespace MyCondo.Application.Features.Property.Gates.Commands.ActivateGate;

public sealed class ActivateGateCommandValidator : AbstractValidator<ActivateGateCommand>
{
    public ActivateGateCommandValidator()
    {
        RuleFor(x => x.GateId).NotEmpty();
    }
}
