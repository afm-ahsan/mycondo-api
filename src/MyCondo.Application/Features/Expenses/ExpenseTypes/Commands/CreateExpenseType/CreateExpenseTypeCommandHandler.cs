using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.CreateExpenseType;

public sealed class CreateExpenseTypeCommandHandler(
    IExpenseTypeRepository expenseTypes,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateExpenseTypeCommandHandler> logger
) : IRequestHandler<CreateExpenseTypeCommand, ExpenseTypeDto>
{
    public async ValueTask<ExpenseTypeDto> Handle(CreateExpenseTypeCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        string normalizedCode = command.Code.Trim().ToUpperInvariant();

        if (await expenseTypes.ExistsByCodeAsync(tenantId, normalizedCode, null, cancellationToken))
        {
            throw new ConflictException($"An expense type with code '{normalizedCode}' already exists.");
        }

        if (await expenseTypes.ExistsByNameAsync(tenantId, command.Name.Trim(), null, cancellationToken))
        {
            throw new ConflictException($"An expense type named '{command.Name}' already exists.");
        }

        ExpenseType expenseType = ExpenseType.Create(
            tenantId, command.Name, normalizedCode, command.Description, command.DisplayOrder, clock.UtcNow);

        expenseTypes.Add(expenseType);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ExpenseType {ExpenseTypeId} '{Name}' created for tenant {TenantId}", expenseType.Id, expenseType.Name, tenantId);

        return new ExpenseTypeDto(
            expenseType.Id.Value, expenseType.Name, expenseType.Code, expenseType.Description,
            expenseType.IsActive, expenseType.DisplayOrder);
    }
}
