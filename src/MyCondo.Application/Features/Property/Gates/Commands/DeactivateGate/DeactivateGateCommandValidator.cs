using FluentValidation;

namespace MyCondo.Application.Features.Property.Gates.Commands.DeactivateGate;

public sealed class DeactivateGateCommandValidator : AbstractValidator<DeactivateGateCommand>
{
    public DeactivateGateCommandValidator()
    {
        RuleFor(x => x.GateId).NotEmpty();
    }
}
