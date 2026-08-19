using FluentValidation;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.DeactivateExpenseCategory;

public sealed class DeactivateExpenseCategoryCommandValidator : AbstractValidator<DeactivateExpenseCategoryCommand>
{
    public DeactivateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.ExpenseCategoryId).NotEmpty();
    }
}
