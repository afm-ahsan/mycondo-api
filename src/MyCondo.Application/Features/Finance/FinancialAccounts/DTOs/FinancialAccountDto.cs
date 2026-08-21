namespace MyCondo.Application.Features.Finance.FinancialAccounts.DTOs;

public sealed record FinancialAccountDto(
    Guid FinancialAccountId,
    string Name,
    string AccountType,
    string? BankName,
    string? BranchName,
    string? AccountNumber,
    Guid ChartOfAccountId,
    Guid? FundId,
    string? FundName,
    string? Notes,
    bool IsActive);
