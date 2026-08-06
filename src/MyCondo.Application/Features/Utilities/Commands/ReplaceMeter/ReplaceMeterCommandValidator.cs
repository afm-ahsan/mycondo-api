using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.ReplaceMeter;

public sealed class ReplaceMeterCommandValidator : AbstractValidator<ReplaceMeterCommand>
{
    public ReplaceMeterCommandValidator()
    {
        RuleFor(x => x.MeterId).NotEmpty();
        RuleFor(x => x.NewMeterNumber).NotEmpty().MaximumLength(60);
    }
}
