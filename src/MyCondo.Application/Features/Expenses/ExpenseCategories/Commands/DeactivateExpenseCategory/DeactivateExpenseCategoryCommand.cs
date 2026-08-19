using Mediator;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.DeactivateExpenseCategory;

public sealed record DeactivateExpenseCategoryCommand(Guid ExpenseCategoryId) : IRequest;
