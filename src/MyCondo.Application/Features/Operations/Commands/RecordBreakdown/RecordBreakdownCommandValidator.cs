using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.RecordBreakdown;

public sealed class RecordBreakdownCommandValidator : AbstractValidator<RecordBreakdownCommand>
{
    public RecordBreakdownCommandValidator()
    {
        RuleFor(x => x.GeneratorId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
    }
}
