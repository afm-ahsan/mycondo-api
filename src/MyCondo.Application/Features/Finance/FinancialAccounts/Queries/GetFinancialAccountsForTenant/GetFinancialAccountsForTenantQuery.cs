using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialAccounts.DTOs;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Queries.GetFinancialAccountsForTenant;

public sealed record GetFinancialAccountsForTenantQuery : IRequest<List<FinancialAccountDto>>, IRequiresFeature
{
    public string FeatureKey => "finance.financial_accounts";
}
