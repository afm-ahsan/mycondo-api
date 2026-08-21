using FluentValidation;

namespace MyCondo.Application.Features.Billing.Commands.WaiveFine;

public sealed class WaiveFineCommandValidator : AbstractValidator<WaiveFineCommand>
{
    public WaiveFineCommandValidator()
    {
        RuleFor(x => x.FineId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
