using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Queries.GetExpenseTypesForTenant;

public sealed class GetExpenseTypesForTenantQueryHandler(
    IExpenseTypeRepository expenseTypes,
    IExpenseCategoryRepository expenseCategories,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetExpenseTypesForTenantQuery, PagedResult<ExpenseTypeDto>>
{
    public async ValueTask<PagedResult<ExpenseTypeDto>> Handle(
        GetExpenseTypesForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PagedResult<ExpenseType> result = await expenseTypes.SearchAsync(
            tenantId, query.Search, query.IsActive, query.Page, query.PageSize, cancellationToken);

        Dictionary<Guid, ExpenseCategory?> categoriesById = [];
        List<ExpenseTypeDto> items = [];

        foreach (ExpenseType t in result.Items)
        {
            ExpenseCategory? category = null;
            if (t.ExpenseCategoryId is ExpenseCategoryId categoryId)
            {
                if (!categoriesById.TryGetValue(categoryId.Value, out category))
                {
                    category = await expenseCategories.GetByIdAsync(categoryId, cancellationToken);
                    categoriesById[categoryId.Value] = category;
                }
            }

            items.Add(new ExpenseTypeDto(
                t.Id.Value, category?.Id.Value, category?.Name, t.Name, t.Code, t.Description, t.IsActive, t.DisplayOrder));
        }

        return new PagedResult<ExpenseTypeDto>(items, result.Page, result.PageSize, result.Total);
    }
}
