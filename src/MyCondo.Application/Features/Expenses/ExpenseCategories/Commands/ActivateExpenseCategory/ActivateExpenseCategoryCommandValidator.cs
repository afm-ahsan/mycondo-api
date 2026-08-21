using FluentValidation;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.ActivateExpenseCategory;

public sealed class ActivateExpenseCategoryCommandValidator : AbstractValidator<ActivateExpenseCategoryCommand>
{
    public ActivateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.ExpenseCategoryId).NotEmpty();
    }
}
