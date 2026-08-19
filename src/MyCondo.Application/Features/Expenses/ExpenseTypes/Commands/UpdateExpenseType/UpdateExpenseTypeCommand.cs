using Mediator;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.UpdateExpenseType;

public sealed record UpdateExpenseTypeCommand(
    Guid ExpenseTypeId,
    Guid ExpenseCategoryId,
    string Name,
    string Code,
    string? Description,
    int DisplayOrder
) : IRequest<ExpenseTypeDto>;
