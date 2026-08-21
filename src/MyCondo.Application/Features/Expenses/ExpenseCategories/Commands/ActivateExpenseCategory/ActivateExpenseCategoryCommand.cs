using Mediator;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.ActivateExpenseCategory;

public sealed record ActivateExpenseCategoryCommand(Guid ExpenseCategoryId) : IRequest;
