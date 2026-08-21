using Mediator;
using MyCondo.Application.Features.Expenses.ExpenseCategories.DTOs;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.UpdateExpenseCategory;

public sealed record UpdateExpenseCategoryCommand(
    Guid ExpenseCategoryId,
    string Name,
    string Code,
    string? Description,
    int DisplayOrder
) : IRequest<ExpenseCategoryDto>;
