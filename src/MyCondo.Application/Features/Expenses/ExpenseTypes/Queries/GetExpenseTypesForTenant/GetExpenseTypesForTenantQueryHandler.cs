using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Queries.GetExpenseTypesForTenant;

public sealed class GetExpenseTypesForTenantQueryHandler(
    IExpenseTypeRepository expenseTypes,
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

        List<ExpenseTypeDto> items = result.Items
            .Select(t => new ExpenseTypeDto(t.Id.Value, t.Name, t.Code, t.Description, t.IsActive, t.DisplayOrder))
            .ToList();

        return new PagedResult<ExpenseTypeDto>(items, result.Page, result.PageSize, result.Total);
    }
}
