using FluentValidation;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.DeactivateExpenseType;

public sealed class DeactivateExpenseTypeCommandValidator : AbstractValidator<DeactivateExpenseTypeCommand>
{
    public DeactivateExpenseTypeCommandValidator()
    {
        RuleFor(x => x.ExpenseTypeId).NotEmpty();
    }
}
