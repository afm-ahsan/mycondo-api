using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.MarkMeterFaulty;

public sealed class MarkMeterFaultyCommandValidator : AbstractValidator<MarkMeterFaultyCommand>
{
    public MarkMeterFaultyCommandValidator()
    {
        RuleFor(x => x.MeterId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
