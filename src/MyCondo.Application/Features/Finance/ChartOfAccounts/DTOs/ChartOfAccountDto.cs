namespace MyCondo.Application.Features.Finance.ChartOfAccounts.DTOs;

/// <summary><c>StatementGroup</c> is the explicit Financial Statements classification, or null if this
/// account has not been classified (a custom account created without one). <c>EffectiveStatementGroup</c>
/// is the classification this account actually reports under — <c>StatementGroup</c> if assigned,
/// otherwise the category's default "Other ..." bucket; it is never null.</summary>
public sealed record ChartOfAccountDto(
    Guid ChartOfAccountId,
    string Code,
    string Name,
    string Category,
    string NormalBalance,
    Guid? ParentAccountId,
    bool IsSystemAccount,
    bool IsActive,
    string? StatementGroup,
    string EffectiveStatementGroup
);
