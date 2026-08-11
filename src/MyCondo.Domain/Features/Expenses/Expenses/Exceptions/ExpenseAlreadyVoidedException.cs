using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Expenses.Expenses.Exceptions;

public sealed class ExpenseAlreadyVoidedException(ExpenseId expenseId)
    : DomainException($"Expense {expenseId} is voided and cannot be edited.")
{
    public ExpenseId ExpenseId { get; } = expenseId;
}
