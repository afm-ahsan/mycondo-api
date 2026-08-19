namespace MyCondo.Application.Features.Finance.ChartOfAccounts.DTOs;

public sealed record ChartOfAccountDto(
    Guid ChartOfAccountId,
    string Code,
    string Name,
    string Category,
    string NormalBalance,
    Guid? ParentAccountId,
    bool IsSystemAccount,
    bool IsActive
);
