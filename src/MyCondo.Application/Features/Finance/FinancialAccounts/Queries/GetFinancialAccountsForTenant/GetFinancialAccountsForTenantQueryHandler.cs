using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.FinancialAccounts.DTOs;
using MyCondo.Application.Features.Finance.FinancialAccounts.Mappings;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Queries.GetFinancialAccountsForTenant;

public sealed class GetFinancialAccountsForTenantQueryHandler(
    IFinancialAccountRepository financialAccounts,
    IFundRepository funds,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetFinancialAccountsForTenantQuery, List<FinancialAccountDto>>
{
    public async ValueTask<List<FinancialAccountDto>> Handle(GetFinancialAccountsForTenantQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<FinancialAccount> accounts = await financialAccounts.GetAllForTenantAsync(tenantId, cancellationToken);
        IReadOnlyList<Fund> allFunds = await funds.GetAllForTenantAsync(tenantId, cancellationToken);
        Dictionary<FundId, string> fundNamesById = allFunds.ToDictionary(f => f.Id, f => f.Name);

        return accounts
            .Select(a => a.ToDto(a.FundId is FundId fundId ? fundNamesById.GetValueOrDefault(fundId) : null))
            .ToList();
    }
}
