using FluentValidation;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.ApproveExpense;

public sealed class ApproveExpenseCommandValidator : AbstractValidator<ApproveExpenseCommand>
{
    public ApproveExpenseCommandValidator()
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
    }
}
