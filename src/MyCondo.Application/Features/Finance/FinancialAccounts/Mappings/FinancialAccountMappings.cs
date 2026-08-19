using MyCondo.Application.Features.Finance.FinancialAccounts.DTOs;
using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Application.Features.Finance.FinancialAccounts.Mappings;

public static class FinancialAccountMappings
{
    public static FinancialAccountDto ToDto(this FinancialAccount account, string? fundName) => new(
        account.Id.Value,
        account.Name,
        account.AccountType.ToString(),
        account.BankName,
        account.BranchName,
        account.AccountNumber,
        account.ChartOfAccountId.Value,
        account.FundId?.Value,
        fundName,
        account.Notes,
        account.IsActive);
}
