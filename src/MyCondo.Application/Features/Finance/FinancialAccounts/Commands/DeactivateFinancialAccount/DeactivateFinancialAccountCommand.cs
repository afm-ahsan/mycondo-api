using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.DeactivateFinancialAccount;

public sealed record DeactivateFinancialAccountCommand(Guid FinancialAccountId) : IRequest, IRequiresFeature
{
    public string FeatureKey => "finance.financial_accounts";
}
