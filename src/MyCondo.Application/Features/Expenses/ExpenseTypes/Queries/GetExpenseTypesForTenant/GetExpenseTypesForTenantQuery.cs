using Mediator;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Queries.GetExpenseTypesForTenant;

public sealed record GetExpenseTypesForTenantQuery(
    string? Search,
    bool? IsActive,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<ExpenseTypeDto>>;
