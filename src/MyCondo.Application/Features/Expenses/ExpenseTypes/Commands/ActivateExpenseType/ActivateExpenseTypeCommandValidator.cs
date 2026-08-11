using FluentValidation;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.ActivateExpenseType;

public sealed class ActivateExpenseTypeCommandValidator : AbstractValidator<ActivateExpenseTypeCommand>
{
    public ActivateExpenseTypeCommandValidator()
    {
        RuleFor(x => x.ExpenseTypeId).NotEmpty();
    }
}
