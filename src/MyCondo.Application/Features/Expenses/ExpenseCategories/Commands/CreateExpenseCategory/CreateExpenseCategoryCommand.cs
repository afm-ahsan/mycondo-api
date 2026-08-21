using Mediator;
using MyCondo.Application.Features.Expenses.ExpenseCategories.DTOs;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.CreateExpenseCategory;

public sealed record CreateExpenseCategoryCommand(
    string Name,
    string Code,
    string? Description,
    int DisplayOrder
) : IRequest<ExpenseCategoryDto>;
