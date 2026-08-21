using Mediator;
using MyCondo.Application.Features.Expenses.ExpenseCategories.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Queries.GetExpenseCategoriesForTenant;

public sealed record GetExpenseCategoriesForTenantQuery(
    string? Search,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<ExpenseCategoryDto>>;
