using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.VoidExpense;

public sealed record VoidExpenseCommand(Guid ExpenseId, string Reason) : IRequest, ILifecycleWriteOperation;
