using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Expenses.Expenses.Queries.GetExpensesForTenant;

public sealed class GetExpensesForTenantQueryHandler(
    IExpenseRepository expenses,
    IExpenseTypeRepository expenseTypes,
    IBuildingRepository buildings,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetExpensesForTenantQuery, PagedResult<ExpenseDto>>
{
    public async ValueTask<PagedResult<ExpenseDto>> Handle(
        GetExpensesForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;
        ExpenseTypeId? expenseTypeId = query.ExpenseTypeId is Guid rawExpenseTypeId ? new ExpenseTypeId(rawExpenseTypeId) : null;
        ExpenseStatus? status = string.IsNullOrWhiteSpace(query.Status) ? null : Enum.Parse<ExpenseStatus>(query.Status);

        PagedResult<Expense> page = await expenses.SearchAsync(
            tenantId, buildingId, expenseTypeId, status, query.FromDate, query.ToDate, query.Page, query.PageSize,
            cancellationToken);

        Dictionary<Guid, Building?> buildingsById = [];
        Dictionary<Guid, ExpenseType?> typesById = [];
        List<ExpenseDto> items = [];

        foreach (Expense expense in page.Items)
        {
            if (!buildingsById.TryGetValue(expense.BuildingId.Value, out Building? building))
            {
                building = await buildings.GetByIdAsync(expense.BuildingId, cancellationToken);
                buildingsById[expense.BuildingId.Value] = building;
            }

            if (!typesById.TryGetValue(expense.ExpenseTypeId.Value, out ExpenseType? expenseType))
            {
                expenseType = await expenseTypes.GetByIdAsync(expense.ExpenseTypeId, cancellationToken);
                typesById[expense.ExpenseTypeId.Value] = expenseType;
            }

            items.Add(new ExpenseDto(
                expense.Id.Value, expense.BuildingId.Value, building?.Name ?? "Unknown",
                expense.ExpenseTypeId.Value, expenseType?.Name ?? "Unknown", expense.ExpenseDate,
                expense.Description, expense.Payee, expense.ReferenceNumber, expense.Amount,
                expense.PaymentMethod.ToString(), expense.Notes, expense.Status.ToString(), expense.VoidReason,
                expense.CreatedBy, expense.CreatedAtUtc, expense.UpdatedAtUtc));
        }

        return new PagedResult<ExpenseDto>(items, page.Page, page.PageSize, page.Total);
    }
}
