using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Commands.BillReading;

public sealed class BillReadingCommandValidator : AbstractValidator<BillReadingCommand>
{
    public BillReadingCommandValidator()
    {
        RuleFor(x => x.ReadingId).NotEmpty();
    }
}
