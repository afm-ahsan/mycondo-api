using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialAccounts.DTOs;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Commands.CreateFinancialAccount;

public sealed record CreateFinancialAccountCommand(
    string Name,
    string AccountType,
    string? BankName,
    string? BranchName,
    string? AccountNumber,
    Guid? FundId,
    string? Notes) : IRequest<FinancialAccountDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "finance.financial_accounts";
}
