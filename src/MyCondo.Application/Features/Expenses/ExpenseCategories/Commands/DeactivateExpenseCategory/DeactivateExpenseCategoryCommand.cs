using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.DeactivateExpenseCategory;

public sealed record DeactivateExpenseCategoryCommand(Guid ExpenseCategoryId) : IRequest, ILifecycleWriteOperation;
