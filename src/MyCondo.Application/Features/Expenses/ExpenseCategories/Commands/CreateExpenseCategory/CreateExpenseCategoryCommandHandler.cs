using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseCategories.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;

namespace MyCondo.Application.Features.Expenses.ExpenseCategories.Commands.CreateExpenseCategory;

public sealed class CreateExpenseCategoryCommandHandler(
    IExpenseCategoryRepository expenseCategories,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateExpenseCategoryCommandHandler> logger
) : IRequestHandler<CreateExpenseCategoryCommand, ExpenseCategoryDto>
{
    public async ValueTask<ExpenseCategoryDto> Handle(CreateExpenseCategoryCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        string normalizedCode = command.Code.Trim().ToUpperInvariant();

        if (await expenseCategories.ExistsByCodeAsync(tenantId, normalizedCode, null, cancellationToken))
        {
            throw new ConflictException($"An expense category with code '{normalizedCode}' already exists.");
        }

        if (await expenseCategories.ExistsByNameAsync(tenantId, command.Name.Trim(), null, cancellationToken))
        {
            throw new ConflictException($"An expense category named '{command.Name}' already exists.");
        }

        ExpenseCategory expenseCategory = ExpenseCategory.Create(
            tenantId, command.Name, normalizedCode, command.Description, command.DisplayOrder, clock.UtcNow);

        expenseCategories.Add(expenseCategory);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ExpenseCategory {ExpenseCategoryId} '{Name}' created for tenant {TenantId}",
            expenseCategory.Id, expenseCategory.Name, tenantId);

        return new ExpenseCategoryDto(
            expenseCategory.Id.Value, expenseCategory.Name, expenseCategory.Code, expenseCategory.Description,
            expenseCategory.IsActive, expenseCategory.DisplayOrder);
    }
}
