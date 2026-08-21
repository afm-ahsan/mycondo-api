using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Queries.GetActiveExpenseTypes;

public sealed class GetActiveExpenseTypesQueryHandler(
    IExpenseTypeRepository expenseTypes,
    IExpenseCategoryRepository expenseCategories,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetActiveExpenseTypesQuery, List<ExpenseTypeDto>>
{
    public async ValueTask<List<ExpenseTypeDto>> Handle(GetActiveExpenseTypesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<ExpenseType> types = await expenseTypes.GetAllActiveForTenantAsync(tenantId, cancellationToken);

        Dictionary<Guid, ExpenseCategory?> categoriesById = [];
        List<ExpenseTypeDto> items = [];

        foreach (ExpenseType t in types)
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

        return items;
    }
}
