using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.ActivateFinancialAccount;

public sealed record ActivateFinancialAccountCommand(Guid FinancialAccountId) : IRequest, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "finance.financial_accounts";
}
