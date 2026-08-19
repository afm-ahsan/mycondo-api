using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseCategories.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.UpdateExpenseCategory;

public sealed class UpdateExpenseCategoryCommandHandler(
    IExpenseCategoryRepository expenseCategories,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<UpdateExpenseCategoryCommandHandler> logger
) : IRequestHandler<UpdateExpenseCategoryCommand, ExpenseCategoryDto>
{
    public async ValueTask<ExpenseCategoryDto> Handle(UpdateExpenseCategoryCommand command, CancellationToken cancellationToken)
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

        string normalizedCode = command.Code.Trim().ToUpperInvariant();

        if (await expenseCategories.ExistsByCodeAsync(tenantId, normalizedCode, expenseCategoryId, cancellationToken))
        {
            throw new ConflictException($"An expense category with code '{normalizedCode}' already exists.");
        }

        if (await expenseCategories.ExistsByNameAsync(tenantId, command.Name.Trim(), expenseCategoryId, cancellationToken))
        {
            throw new ConflictException($"An expense category named '{command.Name}' already exists.");
        }

        expenseCategory.Update(command.Name, normalizedCode, command.Description, command.DisplayOrder, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ExpenseCategory {ExpenseCategoryId} updated for tenant {TenantId}", expenseCategoryId, tenantId);

        return new ExpenseCategoryDto(
            expenseCategory.Id.Value, expenseCategory.Name, expenseCategory.Code, expenseCategory.Description,
            expenseCategory.IsActive, expenseCategory.DisplayOrder);
    }
}
