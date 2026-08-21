using Mediator;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.PayExpense;

/// <summary>Records a supplier payment against a previously unpaid, posted Expense — "Dr AccountsPayable
/// / Cr Bank/Cash".</summary>
public sealed record PayExpenseCommand(Guid ExpenseId) : IRequest<ExpenseDto>;
