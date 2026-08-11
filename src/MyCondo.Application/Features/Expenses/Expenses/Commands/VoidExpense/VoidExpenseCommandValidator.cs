using FluentValidation;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.VoidExpense;

public sealed class VoidExpenseCommandValidator : AbstractValidator<VoidExpenseCommand>
{
    public VoidExpenseCommandValidator()
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
