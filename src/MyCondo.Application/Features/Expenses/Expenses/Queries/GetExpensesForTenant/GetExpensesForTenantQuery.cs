using Mediator;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Expenses.Expenses.Queries.GetExpensesForTenant;

public sealed record GetExpensesForTenantQuery(
    Guid? BuildingId,
    Guid? ExpenseTypeId,
    string? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<ExpenseDto>>;
