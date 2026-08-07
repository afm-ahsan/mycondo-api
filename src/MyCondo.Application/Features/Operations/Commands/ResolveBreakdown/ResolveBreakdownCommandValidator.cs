using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.ResolveBreakdown;

public sealed class ResolveBreakdownCommandValidator : AbstractValidator<ResolveBreakdownCommand>
{
    public ResolveBreakdownCommandValidator()
    {
        RuleFor(x => x.GeneratorBreakdownRecordId).NotEmpty();
        RuleFor(x => x.Resolution).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost is not null);
    }
}
