using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.ActivateExpenseType;

public sealed record ActivateExpenseTypeCommand(Guid ExpenseTypeId) : IRequest, ILifecycleWriteOperation;
