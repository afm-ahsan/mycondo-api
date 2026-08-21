using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseCategories.DTOs;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Queries.GetActiveExpenseCategories;

public sealed class GetActiveExpenseCategoriesQueryHandler(
    IExpenseCategoryRepository expenseCategories,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetActiveExpenseCategoriesQuery, List<ExpenseCategoryDto>>
{
    public async ValueTask<List<ExpenseCategoryDto>> Handle(GetActiveExpenseCategoriesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<ExpenseCategory> categories = await expenseCategories.GetAllActiveForTenantAsync(tenantId, cancellationToken);

        return categories
            .Select(c => new ExpenseCategoryDto(c.Id.Value, c.Name, c.Code, c.Description, c.IsActive, c.DisplayOrder))
            .ToList();
    }
}
