using Mediator;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.VoidExpense;

public sealed record VoidExpenseCommand(Guid ExpenseId, string Reason) : IRequest;
