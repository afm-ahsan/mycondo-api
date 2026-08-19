using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Expenses.Expenses.Exceptions;

/// <summary>Thrown when an Expense state-transition method (<see cref="Expense.MarkPosted"/>,
/// <see cref="Expense.MarkPaid"/>) is called from a status it cannot legally transition from — e.g.
/// approving an already-posted Expense, or recording a supplier payment for one that was paid
/// immediately at recording time.</summary>
public sealed class ExpenseInvalidStateTransitionException(ExpenseId expenseId, ExpenseStatus status, string attemptedTransition)
    : DomainException($"Expense {expenseId} is {status} and cannot {attemptedTransition}.")
{
    public ExpenseId ExpenseId { get; } = expenseId;
    public ExpenseStatus Status { get; } = status;
}
