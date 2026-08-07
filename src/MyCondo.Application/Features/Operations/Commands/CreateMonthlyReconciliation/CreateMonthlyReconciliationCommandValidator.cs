using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.CreateMonthlyReconciliation;

public sealed class CreateMonthlyReconciliationCommandValidator : AbstractValidator<CreateMonthlyReconciliationCommand>
{
    public CreateMonthlyReconciliationCommandValidator()
    {
        RuleFor(x => x.CylinderType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
