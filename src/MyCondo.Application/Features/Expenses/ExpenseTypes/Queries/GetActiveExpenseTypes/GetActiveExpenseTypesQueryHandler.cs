using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Queries.GetActiveExpenseTypes;

public sealed class GetActiveExpenseTypesQueryHandler(
    IExpenseTypeRepository expenseTypes,
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

        return types
            .Select(t => new ExpenseTypeDto(t.Id.Value, t.Name, t.Code, t.Description, t.IsActive, t.DisplayOrder))
            .ToList();
    }
}
