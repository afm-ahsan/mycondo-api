using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.UpdateExpenseType;

public sealed class UpdateExpenseTypeCommandHandler(
    IExpenseTypeRepository expenseTypes,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<UpdateExpenseTypeCommandHandler> logger
) : IRequestHandler<UpdateExpenseTypeCommand, ExpenseTypeDto>
{
    public async ValueTask<ExpenseTypeDto> Handle(UpdateExpenseTypeCommand command, CancellationToken cancellationToken)
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

        string normalizedCode = command.Code.Trim().ToUpperInvariant();

        if (await expenseTypes.ExistsByCodeAsync(tenantId, normalizedCode, expenseTypeId, cancellationToken))
        {
            throw new ConflictException($"An expense type with code '{normalizedCode}' already exists.");
        }

        if (await expenseTypes.ExistsByNameAsync(tenantId, command.Name.Trim(), expenseTypeId, cancellationToken))
        {
            throw new ConflictException($"An expense type named '{command.Name}' already exists.");
        }

        expenseType.Update(command.Name, normalizedCode, command.Description, command.DisplayOrder, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("ExpenseType {ExpenseTypeId} updated for tenant {TenantId}", expenseTypeId, tenantId);

        return new ExpenseTypeDto(
            expenseType.Id.Value, expenseType.Name, expenseType.Code, expenseType.Description,
            expenseType.IsActive, expenseType.DisplayOrder);
    }
}
