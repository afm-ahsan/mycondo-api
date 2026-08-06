using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.FinalizeReading;

public sealed class FinalizeReadingCommandValidator : AbstractValidator<FinalizeReadingCommand>
{
    public FinalizeReadingCommandValidator()
    {
        RuleFor(x => x.ReadingId).NotEmpty();
    }
}
