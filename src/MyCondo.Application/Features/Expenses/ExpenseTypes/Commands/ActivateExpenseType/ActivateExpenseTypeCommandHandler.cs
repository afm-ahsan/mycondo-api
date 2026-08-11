using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.ActivateExpenseType;

public sealed class ActivateExpenseTypeCommandHandler(
    IExpenseTypeRepository expenseTypes,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ActivateExpenseTypeCommandHandler> logger
) : IRequestHandler<ActivateExpenseTypeCommand>
{
    public async ValueTask<Unit> Handle(ActivateExpenseTypeCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ExpenseTypeId expenseTypeId = new(command.ExpenseTypeId);
        ExpenseType expenseType = await expenseTypes.GetByIdAsync(expenseTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ExpenseType), command.ExpenseTypeId);

        if (expenseType.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ExpenseType), command.ExpenseTypeId);
        }

        expenseType.Activate(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("ExpenseType {ExpenseTypeId} activated for tenant {TenantId}", expenseTypeId, tenantId);

        return Unit.Value;
    }
}
