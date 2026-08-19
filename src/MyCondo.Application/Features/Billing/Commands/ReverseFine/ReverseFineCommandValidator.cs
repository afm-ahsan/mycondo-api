using FluentValidation;

namespace MyCondo.Application.Features.Billing.Commands.ReverseFine;

public sealed class ReverseFineCommandValidator : AbstractValidator<ReverseFineCommand>
{
    public ReverseFineCommandValidator()
    {
        RuleFor(x => x.FineId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
