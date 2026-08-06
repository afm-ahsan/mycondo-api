using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.CorrectReading;

public sealed class CorrectReadingCommandValidator : AbstractValidator<CorrectReadingCommand>
{
    public CorrectReadingCommandValidator()
    {
        RuleFor(x => x.ReadingId).NotEmpty();
        RuleFor(x => x.PreviousReading).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PresentReading).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReadingDate).NotEmpty();
        RuleFor(x => x.OverrideReason).MaximumLength(500);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
