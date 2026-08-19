using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Expenses.Expenses.Exceptions;

/// <summary>Thrown by <see cref="Expense.Update"/> once an Expense has posted accounting consequences
/// (<see cref="ExpenseStatus.Posted"/>/<see cref="ExpenseStatus.Paid"/>) or has been voided — posted
/// financial values are immutable (root CLAUDE.md's "do not mutate posted financial ledger entries"); a
/// mistake is corrected by voiding (which posts a reversal) and recording a new Expense, never by
/// editing one back.</summary>
public sealed class ExpenseNotEditableException(ExpenseId expenseId, ExpenseStatus status)
    : DomainException($"Expense {expenseId} is {status} and cannot be edited.")
{
    public ExpenseId ExpenseId { get; } = expenseId;
    public ExpenseStatus Status { get; } = status;
}
