using Mediator;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.DeactivateExpenseType;

public sealed record DeactivateExpenseTypeCommand(Guid ExpenseTypeId) : IRequest;
