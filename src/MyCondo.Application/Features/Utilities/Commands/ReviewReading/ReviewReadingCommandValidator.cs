using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.ReviewReading;

public sealed class ReviewReadingCommandValidator : AbstractValidator<ReviewReadingCommand>
{
    public ReviewReadingCommandValidator()
    {
        RuleFor(x => x.ReadingId).NotEmpty();
    }
}
