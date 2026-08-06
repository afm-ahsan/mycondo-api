using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.AssignMeter;

public sealed class AssignMeterCommandValidator : AbstractValidator<AssignMeterCommand>
{
    public AssignMeterCommandValidator()
    {
        RuleFor(x => x.MeterId).NotEmpty();
        RuleFor(x => x.FlatId).NotEmpty();
    }
}
