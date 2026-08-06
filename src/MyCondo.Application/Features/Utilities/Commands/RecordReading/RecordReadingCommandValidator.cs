using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.RecordReading;

public sealed class RecordReadingCommandValidator : AbstractValidator<RecordReadingCommand>
{
    public RecordReadingCommandValidator()
    {
        RuleFor(x => x.MeterId).NotEmpty();
        RuleFor(x => x.PeriodStart).NotEmpty();
        RuleFor(x => x.PeriodEnd).NotEmpty();
        RuleFor(x => x.PeriodEnd).GreaterThanOrEqualTo(x => x.PeriodStart)
            .WithMessage("PeriodEnd must not precede PeriodStart.");
        RuleFor(x => x.PreviousReading).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PresentReading).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReadingDate).NotEmpty();
        RuleFor(x => x.OverrideReason).MaximumLength(500);
    }
}
