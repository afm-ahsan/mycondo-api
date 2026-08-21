using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.ChartOfAccounts.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.ChartOfAccounts.Commands.CreateChartOfAccount;

public sealed class CreateChartOfAccountCommandHandler(
    IChartOfAccountRepository chartOfAccounts,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<CreateChartOfAccountCommandHandler> logger
) : IRequestHandler<CreateChartOfAccountCommand, ChartOfAccountDto>
{
    public async ValueTask<ChartOfAccountDto> Handle(CreateChartOfAccountCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        string code = command.Code.Trim();
        if (await chartOfAccounts.ExistsForCodeAsync(tenantId, code, cancellationToken))
        {
            throw new ConflictException($"An account with code '{code}' already exists.");
        }

        AccountCategory category = Enum.Parse<AccountCategory>(command.Category);
        LedgerDirection normalBalance = Enum.Parse<LedgerDirection>(command.NormalBalance);
        ChartOfAccountId? parentAccountId = command.ParentAccountId is Guid parentId ? new ChartOfAccountId(parentId) : null;

        ChartOfAccount account = ChartOfAccount.Create(
            tenantId, code, command.Name, category, normalBalance, parentAccountId, isSystemAccount: false);

        chartOfAccounts.Add(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Chart of account {ChartOfAccountId} '{Code}' created for tenant {TenantId}", account.Id, code, tenantId);

        return new ChartOfAccountDto(
            account.Id.Value, account.Code, account.Name, account.Category.ToString(), account.NormalBalance.ToString(),
            account.ParentAccountId?.Value, account.IsSystemAccount, account.IsActive);
    }
}
