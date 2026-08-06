using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.ReactivateMeter;

public sealed class ReactivateMeterCommandValidator : AbstractValidator<ReactivateMeterCommand>
{
    public ReactivateMeterCommandValidator()
    {
        RuleFor(x => x.MeterId).NotEmpty();
    }
}
