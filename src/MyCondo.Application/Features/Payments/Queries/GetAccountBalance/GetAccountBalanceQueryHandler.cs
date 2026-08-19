using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.ResidentAccounts;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Payments.Queries.GetAccountBalance;

public sealed class GetAccountBalanceQueryHandler(
    IResidentAccountRepository accounts,
    ILedgerEntryRepository ledgerEntries,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetAccountBalanceQuery, AccountBalanceDto>
{
    public async ValueTask<AccountBalanceDto> Handle(GetAccountBalanceQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(query.FlatId);
        ResidentAccount? account = await accounts.GetByFlatIdAsync(tenantId, flatId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException(nameof(ResidentAccount), query.FlatId);
        }

        decimal balance = await ledgerEntries.GetReceivableBalanceForFlatAsync(tenantId, flatId, cancellationToken);
        decimal advanceBalance = await ledgerEntries.GetAdvanceBalanceForFlatAsync(tenantId, flatId, cancellationToken);

        return new AccountBalanceDto(query.FlatId, balance, advanceBalance);
    }
}
