using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.DeactivateExpenseCategory;

public sealed class DeactivateExpenseCategoryCommandHandler(
    IExpenseCategoryRepository expenseCategories,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<DeactivateExpenseCategoryCommandHandler> logger
) : IRequestHandler<DeactivateExpenseCategoryCommand>
{
    public async ValueTask<Unit> Handle(DeactivateExpenseCategoryCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ExpenseCategoryId expenseCategoryId = new(command.ExpenseCategoryId);
        ExpenseCategory expenseCategory = await expenseCategories.GetByIdAsync(expenseCategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(ExpenseCategory), command.ExpenseCategoryId);

        if (expenseCategory.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ExpenseCategory), command.ExpenseCategoryId);
        }

        expenseCategory.Deactivate(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ExpenseCategory {ExpenseCategoryId} deactivated for tenant {TenantId}", expenseCategoryId, tenantId);

        return Unit.Value;
    }
}
