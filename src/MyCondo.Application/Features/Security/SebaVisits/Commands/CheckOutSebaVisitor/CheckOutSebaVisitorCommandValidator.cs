using FluentValidation;

namespace MyCondo.Application.Features.Security.SebaVisits.Commands.CheckOutSebaVisitor;

public sealed class CheckOutSebaVisitorCommandValidator : AbstractValidator<CheckOutSebaVisitorCommand>
{
    public CheckOutSebaVisitorCommandValidator()
    {
        RuleFor(x => x.AccessSessionId).NotEmpty();
        RuleFor(x => x.ExitGateId).NotEmpty();
        RuleFor(x => x.ServiceOutcome).MaximumLength(500);
    }
}
