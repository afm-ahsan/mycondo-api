using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialAccounts.DTOs;
using MyCondo.Application.Features.Finance.FinancialAccounts.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.CreateFinancialAccount;

/// <summary>
/// Creates a Financial Account — a physical money location — and, alongside it, its own dedicated
/// asset <see cref="ChartOfAccount"/> as a child of the tenant's single system <c>CashOrBank</c> account
/// (see <see cref="FinancialAccount"/>'s doc comment). Never reparents/renames the system account itself.
/// </summary>
public sealed class CreateFinancialAccountCommandHandler(
    IFinancialAccountRepository financialAccounts,
    IChartOfAccountRepository chartOfAccounts,
    IAccountMappingRepository accountMappings,
    IFundRepository funds,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<CreateFinancialAccountCommandHandler> logger
) : IRequestHandler<CreateFinancialAccountCommand, FinancialAccountDto>
{
    public async ValueTask<FinancialAccountDto> Handle(CreateFinancialAccountCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ChartOfAccountId? cashOrBankAccountId = await accountMappings.ResolveAccountIdAsync(
            tenantId, LedgerAccountType.CashOrBank.ToString(), cancellationToken);
        if (cashOrBankAccountId is null)
        {
            throw new ConflictException("The tenant's Cash/Bank account mapping has not been configured yet.");
        }

        FinancialAccountType accountType = Enum.Parse<FinancialAccountType>(command.AccountType);
        FundId? fundId = command.FundId is Guid fundGuid ? new FundId(fundGuid) : null;
        Fund? fund = fundId is FundId f ? await funds.GetByIdAsync(f, cancellationToken) : null;
        if (fundId is not null && (fund is null || fund.TenantId != tenantId))
        {
            throw new NotFoundException(nameof(Fund), command.FundId!.Value);
        }

        string ledgerAccountCode = $"1000-{Guid.CreateVersion7().ToString("N")[..8]}".ToUpperInvariant();

        ChartOfAccount ledgerAccount = ChartOfAccount.Create(
            tenantId, ledgerAccountCode, command.Name, AccountCategory.Asset, LedgerDirection.Debit,
            parentAccountId: cashOrBankAccountId.Value, isSystemAccount: false);
        chartOfAccounts.Add(ledgerAccount);

        FinancialAccount account = FinancialAccount.Create(
            tenantId, command.Name, accountType, command.BankName, command.BranchName, command.AccountNumber,
            ledgerAccount.Id, fundId, command.Notes);
        financialAccounts.Add(account);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Financial Account {FinancialAccountId} '{Name}' created for tenant {TenantId}", account.Id, account.Name, tenantId);

        return account.ToDto(fund?.Name);
    }
}
