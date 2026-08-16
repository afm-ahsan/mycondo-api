using FluentValidation;

namespace MyCondo.Application.Features.Property.Gates.Commands.UpdateGate;

public sealed class UpdateGateCommandValidator : AbstractValidator<UpdateGateCommand>
{
    public UpdateGateCommandValidator()
    {
        RuleFor(x => x.GateId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
