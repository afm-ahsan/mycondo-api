using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialAccounts.DTOs;
using MyCondo.Application.Features.Finance.FinancialAccounts.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.UpdateFinancialAccount;

public sealed class UpdateFinancialAccountCommandHandler(
    IFinancialAccountRepository financialAccounts,
    IFundRepository funds,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<UpdateFinancialAccountCommandHandler> logger
) : IRequestHandler<UpdateFinancialAccountCommand, FinancialAccountDto>
{
    public async ValueTask<FinancialAccountDto> Handle(UpdateFinancialAccountCommand command, CancellationToken cancellationToken)
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

        FundId? fundId = command.FundId is Guid fundGuid ? new FundId(fundGuid) : null;
        Fund? fund = fundId is FundId f ? await funds.GetByIdAsync(f, cancellationToken) : null;
        if (fundId is not null && (fund is null || fund.TenantId != tenantId))
        {
            throw new NotFoundException(nameof(Fund), command.FundId!.Value);
        }

        account.Update(command.Name, command.BankName, command.BranchName, command.AccountNumber, fundId, command.Notes);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Financial Account {FinancialAccountId} updated for tenant {TenantId}", financialAccountId, tenantId);

        return account.ToDto(fund?.Name);
    }
}
