using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Application.Features.Expenses.Expenses.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.UpdateExpense;

public sealed class UpdateExpenseCommandHandler(
    IExpenseRepository expenses,
    IExpenseTypeRepository expenseTypes,
    IExpenseCategoryRepository expenseCategories,
    IBuildingRepository buildings,
    IFundRepository funds,
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

        if (!currentUser.HasPermissionForBuilding(ExpenseManagePermission, expense.BuildingId?.Value))
        {
            throw new ForbiddenException("You do not have permission to manage expenses for this Building.");
        }

        BuildingId? buildingId = command.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;
        Building? building = null;

        if (buildingId is BuildingId bId)
        {
            building = await buildings.GetByIdAsync(bId, cancellationToken)
                ?? throw new NotFoundException(nameof(Building), command.BuildingId!.Value);

            if (building.TenantId != tenantId)
            {
                throw new NotFoundException(nameof(Building), command.BuildingId!.Value);
            }
        }

        if (!currentUser.HasPermissionForBuilding(ExpenseManagePermission, buildingId?.Value))
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

        ExpenseCategory? expenseCategory = expenseType.ExpenseCategoryId is ExpenseCategoryId categoryId
            ? await expenseCategories.GetByIdAsync(categoryId, cancellationToken)
            : null;

        FundId? fundId = command.FundId is Guid rawFundId ? new FundId(rawFundId) : null;
        Fund? fund = null;

        if (fundId is FundId fId)
        {
            fund = await funds.GetByIdAsync(fId, cancellationToken)
                ?? throw new NotFoundException(nameof(Fund), command.FundId!.Value);

            if (fund.TenantId != tenantId)
            {
                throw new NotFoundException(nameof(Fund), command.FundId!.Value);
            }
        }

        PaymentMethod paymentMethod = Enum.Parse<PaymentMethod>(command.PaymentMethod);

        expense.Update(
            buildingId, expenseTypeId, fundId, command.ExpenseDate, command.AccountingDate, command.Description,
            command.Payee, command.ReferenceNumber, command.Amount, command.IsPaid, paymentMethod, command.Notes,
            clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Expense {ExpenseId} updated for tenant {TenantId}", expenseId, tenantId);

        return expense.ToDto(
            building?.Name, expenseType.Name, expenseCategory?.Id.Value, expenseCategory?.Name, fund?.Name);
    }
}
