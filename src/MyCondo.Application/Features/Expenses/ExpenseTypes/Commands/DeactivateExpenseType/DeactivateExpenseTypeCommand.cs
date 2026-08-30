using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.DeactivateExpenseType;

public sealed record DeactivateExpenseTypeCommand(Guid ExpenseTypeId) : IRequest, ILifecycleWriteOperation;
