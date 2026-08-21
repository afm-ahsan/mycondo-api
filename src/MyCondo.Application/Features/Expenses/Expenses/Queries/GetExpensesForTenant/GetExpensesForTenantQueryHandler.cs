using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Application.Features.Expenses.Expenses.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Expenses.Expenses.Queries.GetExpensesForTenant;

public sealed class GetExpensesForTenantQueryHandler(
    IExpenseRepository expenses,
    IExpenseTypeRepository expenseTypes,
    IExpenseCategoryRepository expenseCategories,
    IBuildingRepository buildings,
    IFundRepository funds,
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
        FundId? fundId = query.FundId is Guid rawFundId ? new FundId(rawFundId) : null;
        ExpenseStatus? status = string.IsNullOrWhiteSpace(query.Status) ? null : Enum.Parse<ExpenseStatus>(query.Status);

        PagedResult<Expense> page = await expenses.SearchAsync(
            tenantId, buildingId, expenseTypeId, fundId, status, query.FromDate, query.ToDate, query.Page,
            query.PageSize, cancellationToken);

        Dictionary<Guid, Building?> buildingsById = [];
        Dictionary<Guid, ExpenseType?> typesById = [];
        Dictionary<Guid, ExpenseCategory?> categoriesById = [];
        Dictionary<Guid, Fund?> fundsById = [];
        List<ExpenseDto> items = [];

        foreach (Expense expense in page.Items)
        {
            Building? building = null;
            if (expense.BuildingId is BuildingId expenseBuildingId)
            {
                if (!buildingsById.TryGetValue(expenseBuildingId.Value, out building))
                {
                    building = await buildings.GetByIdAsync(expenseBuildingId, cancellationToken);
                    buildingsById[expenseBuildingId.Value] = building;
                }
            }

            if (!typesById.TryGetValue(expense.ExpenseTypeId.Value, out ExpenseType? expenseType))
            {
                expenseType = await expenseTypes.GetByIdAsync(expense.ExpenseTypeId, cancellationToken);
                typesById[expense.ExpenseTypeId.Value] = expenseType;
            }

            ExpenseCategory? expenseCategory = null;
            if (expenseType?.ExpenseCategoryId is ExpenseCategoryId categoryId)
            {
                if (!categoriesById.TryGetValue(categoryId.Value, out expenseCategory))
                {
                    expenseCategory = await expenseCategories.GetByIdAsync(categoryId, cancellationToken);
                    categoriesById[categoryId.Value] = expenseCategory;
                }
            }

            Fund? fund = null;
            if (expense.FundId is FundId expenseFundId)
            {
                if (!fundsById.TryGetValue(expenseFundId.Value, out fund))
                {
                    fund = await funds.GetByIdAsync(expenseFundId, cancellationToken);
                    fundsById[expenseFundId.Value] = fund;
                }
            }

            items.Add(expense.ToDto(
                building?.Name, expenseType?.Name ?? "Unknown", expenseCategory?.Id.Value, expenseCategory?.Name,
                fund?.Name));
        }

        return new PagedResult<ExpenseDto>(items, page.Page, page.PageSize, page.Total);
    }
}
