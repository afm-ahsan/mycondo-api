using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseCategories.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Queries.GetExpenseCategoriesForTenant;

public sealed class GetExpenseCategoriesForTenantQueryHandler(
    IExpenseCategoryRepository expenseCategories,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetExpenseCategoriesForTenantQuery, PagedResult<ExpenseCategoryDto>>
{
    public async ValueTask<PagedResult<ExpenseCategoryDto>> Handle(
        GetExpenseCategoriesForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<ExpenseCategory> result = await expenseCategories.SearchAsync(
            tenantId, query.Search, query.IsActive, query.Page, query.PageSize, cancellationToken);

        List<ExpenseCategoryDto> items = result.Items
            .Select(c => new ExpenseCategoryDto(c.Id.Value, c.Name, c.Code, c.Description, c.IsActive, c.DisplayOrder))
            .ToList();

        return new PagedResult<ExpenseCategoryDto>(items, result.Page, result.PageSize, result.Total);
    }
}
