using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.VoidExpense;

public sealed class VoidExpenseCommandHandler(
    IExpenseRepository expenses,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<VoidExpenseCommandHandler> logger
) : IRequestHandler<VoidExpenseCommand>
{
    private const string ExpenseManagePermission = "expense.manage";

    public async ValueTask<Unit> Handle(VoidExpenseCommand command, CancellationToken cancellationToken)
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

        expense.Void(command.Reason, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Expense {ExpenseId} voided for tenant {TenantId}", expenseId, tenantId);

        return Unit.Value;
    }
}
