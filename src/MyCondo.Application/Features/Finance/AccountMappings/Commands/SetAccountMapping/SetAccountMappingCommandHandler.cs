using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.AccountMappings.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Application.Features.Finance.AccountMappings.Commands.SetAccountMapping;

public sealed class SetAccountMappingCommandHandler(
    IAccountMappingRepository accountMappings,
    IChartOfAccountRepository chartOfAccounts,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<SetAccountMappingCommandHandler> logger
) : IRequestHandler<SetAccountMappingCommand, AccountMappingDto>
{
    public async ValueTask<AccountMappingDto> Handle(SetAccountMappingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ChartOfAccountId accountId = new(command.ChartOfAccountId);
        ChartOfAccount account = await chartOfAccounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(ChartOfAccount), command.ChartOfAccountId);
        if (account.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ChartOfAccount), command.ChartOfAccountId);
        }

        AccountMapping? existing = await accountMappings.GetByRoleAsync(tenantId, command.PostingRole, cancellationToken);
        AccountMapping mapping;
        if (existing is null)
        {
            mapping = AccountMapping.Create(tenantId, command.PostingRole, accountId);
            accountMappings.Add(mapping);
        }
        else
        {
            existing.Remap(accountId);
            mapping = existing;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Account mapping for role '{PostingRole}' set to account {ChartOfAccountId}, tenant {TenantId}",
            command.PostingRole, accountId, tenantId);

        return new AccountMappingDto(mapping.Id.Value, mapping.PostingRole, mapping.ChartOfAccountId.Value);
    }
}
