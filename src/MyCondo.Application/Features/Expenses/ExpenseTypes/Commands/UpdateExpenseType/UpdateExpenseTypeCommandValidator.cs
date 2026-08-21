using FluentValidation;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.UpdateExpenseType;

public sealed class UpdateExpenseTypeCommandValidator : AbstractValidator<UpdateExpenseTypeCommand>
{
    public UpdateExpenseTypeCommandValidator()
    {
        RuleFor(x => x.ExpenseTypeId).NotEmpty();
        RuleFor(x => x.ExpenseCategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
