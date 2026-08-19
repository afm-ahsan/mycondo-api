using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Application.Features.Expenses.Expenses.Mappings;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Expenses.Expenses.Queries.GetExpenseById;

public sealed class GetExpenseByIdQueryHandler(
    IExpenseRepository expenses,
    IExpenseTypeRepository expenseTypes,
    IExpenseCategoryRepository expenseCategories,
    IBuildingRepository buildings,
    IFundRepository funds,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetExpenseByIdQuery, ExpenseDto>
{
    public async ValueTask<ExpenseDto> Handle(GetExpenseByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ExpenseId expenseId = new(query.ExpenseId);
        Expense expense = await expenses.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Expense), query.ExpenseId);

        if (expense.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Expense), query.ExpenseId);
        }

        Building? building = expense.BuildingId is BuildingId buildingId
            ? await buildings.GetByIdAsync(buildingId, cancellationToken)
            : null;
        ExpenseType? expenseType = await expenseTypes.GetByIdAsync(expense.ExpenseTypeId, cancellationToken);
        ExpenseCategory? expenseCategory = expenseType?.ExpenseCategoryId is ExpenseCategoryId categoryId
            ? await expenseCategories.GetByIdAsync(categoryId, cancellationToken)
            : null;
        Fund? fund = expense.FundId is FundId fundId ? await funds.GetByIdAsync(fundId, cancellationToken) : null;

        return expense.ToDto(
            building?.Name, expenseType?.Name ?? "Unknown", expenseCategory?.Id.Value, expenseCategory?.Name,
            fund?.Name);
    }
}
