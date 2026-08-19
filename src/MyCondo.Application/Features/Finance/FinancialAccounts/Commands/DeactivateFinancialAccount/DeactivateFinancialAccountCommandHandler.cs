using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.DeactivateFinancialAccount;

public sealed class DeactivateFinancialAccountCommandHandler(
    IFinancialAccountRepository financialAccounts,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<DeactivateFinancialAccountCommandHandler> logger
) : IRequestHandler<DeactivateFinancialAccountCommand>
{
    public async ValueTask<Unit> Handle(DeactivateFinancialAccountCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FinancialAccountId financialAccountId = new(command.FinancialAccountId);
        FinancialAccount account = await financialAccounts.GetByIdAsync(financialAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(FinancialAccount), command.FinancialAccountId);

        if (account.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(FinancialAccount), command.FinancialAccountId);
        }

        account.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Financial Account {FinancialAccountId} deactivated for tenant {TenantId}", financialAccountId, tenantId);

        return Unit.Value;
    }
}
