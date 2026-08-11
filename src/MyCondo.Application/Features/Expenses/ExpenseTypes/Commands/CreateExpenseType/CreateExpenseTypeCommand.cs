using Mediator;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.CreateExpenseType;

public sealed record CreateExpenseTypeCommand(
    string Name,
    string Code,
    string? Description,
    int DisplayOrder
) : IRequest<ExpenseTypeDto>;
