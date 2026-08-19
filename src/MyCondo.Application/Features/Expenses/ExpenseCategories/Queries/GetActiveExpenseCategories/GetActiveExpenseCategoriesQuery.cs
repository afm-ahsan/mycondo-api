using Mediator;
using MyCondo.Application.Features.Expenses.ExpenseCategories.DTOs;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Queries.GetActiveExpenseCategories;

public sealed record GetActiveExpenseCategoriesQuery : IRequest<List<ExpenseCategoryDto>>;
