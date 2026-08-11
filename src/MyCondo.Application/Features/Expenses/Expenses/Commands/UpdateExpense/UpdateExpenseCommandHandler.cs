using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.UpdateExpense;

public sealed class UpdateExpenseCommandHandler(
    IExpenseRepository expenses,
    IExpenseTypeRepository expenseTypes,
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<UpdateExpenseCommandHandler> logger
) : IRequestHandler<UpdateExpenseCommand, ExpenseDto>
{
    private const string ExpenseManagePermission = "expense.manage";

    public async ValueTask<ExpenseDto> Handle(UpdateExpenseCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ExpenseId expenseId = new(command.ExpenseId);
        Expense expense = await expenses.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Expense), command.ExpenseId);

        if (expense.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Expense), command.ExpenseId);
        }

        if (!currentUser.HasPermissionForBuilding(ExpenseManagePermission, expense.BuildingId.Value))
        {
            throw new ForbiddenException("You do not have permission to manage expenses for this Building.");
        }

        BuildingId buildingId = new(command.BuildingId);
        Building building = await buildings.GetByIdAsync(buildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Building), command.BuildingId);

        if (building.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Building), command.BuildingId);
        }

        if (!currentUser.HasPermissionForBuilding(ExpenseManagePermission, buildingId.Value))
        {
            throw new ForbiddenException("You do not have permission to manage expenses for this Building.");
        }

        ExpenseTypeId expenseTypeId = new(command.ExpenseTypeId);
        ExpenseType expenseType = await expenseTypes.GetByIdAsync(expenseTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ExpenseType), command.ExpenseTypeId);

        if (expenseType.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ExpenseType), command.ExpenseTypeId);
        }

        PaymentMethod paymentMethod = Enum.Parse<PaymentMethod>(command.PaymentMethod);

        expense.Update(
            buildingId, expenseTypeId, command.ExpenseDate, command.Description, command.Payee,
            command.ReferenceNumber, command.Amount, paymentMethod, command.Notes, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Expense {ExpenseId} updated for tenant {TenantId}", expenseId, tenantId);

        return new ExpenseDto(
            expense.Id.Value, building.Id.Value, building.Name, expenseType.Id.Value, expenseType.Name,
            expense.ExpenseDate, expense.Description, expense.Payee, expense.ReferenceNumber, expense.Amount,
            expense.PaymentMethod.ToString(), expense.Notes, expense.Status.ToString(), expense.VoidReason,
            expense.CreatedBy, expense.CreatedAtUtc, expense.UpdatedAtUtc);
    }
}
