using FluentValidation;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.PayExpense;

public sealed class PayExpenseCommandValidator : AbstractValidator<PayExpenseCommand>
{
    public PayExpenseCommandValidator()
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
    }
}
