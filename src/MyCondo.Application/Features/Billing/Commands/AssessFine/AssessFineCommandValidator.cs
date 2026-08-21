using FluentValidation;

namespace MyCondo.Application.Features.Billing.Commands.AssessFine;

public sealed class AssessFineCommandValidator : AbstractValidator<AssessFineCommand>
{
    public AssessFineCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BusinessDate).NotEmpty();
    }
}
