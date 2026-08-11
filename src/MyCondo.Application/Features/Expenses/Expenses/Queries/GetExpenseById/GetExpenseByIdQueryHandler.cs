using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Expenses.Expenses.Queries.GetExpenseById;

public sealed class GetExpenseByIdQueryHandler(
    IExpenseRepository expenses,
    IExpenseTypeRepository expenseTypes,
    IBuildingRepository buildings,
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

        Building? building = await buildings.GetByIdAsync(expense.BuildingId, cancellationToken);
        ExpenseType? expenseType = await expenseTypes.GetByIdAsync(expense.ExpenseTypeId, cancellationToken);

        return new ExpenseDto(
            expense.Id.Value, expense.BuildingId.Value, building?.Name ?? "Unknown", expense.ExpenseTypeId.Value,
            expenseType?.Name ?? "Unknown", expense.ExpenseDate, expense.Description, expense.Payee,
            expense.ReferenceNumber, expense.Amount, expense.PaymentMethod.ToString(), expense.Notes,
            expense.Status.ToString(), expense.VoidReason, expense.CreatedBy, expense.CreatedAtUtc, expense.UpdatedAtUtc);
    }
}
