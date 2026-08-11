using FluentValidation;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.CreateExpenseType;

public sealed class CreateExpenseTypeCommandValidator : AbstractValidator<CreateExpenseTypeCommand>
{
    public CreateExpenseTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
