namespace MyCondo.Domain.Features.Finance.ChartOfAccounts;

/// <summary>Top-level grouping from the master architecture's suggested initial account structure
/// (Assets / Liabilities / Funds-Equity / Income / Expenses) — drives <see cref="ChartOfAccount"/>'s
/// default normal-balance direction and financial-statement placement.</summary>
public enum AccountCategory
{
    Asset = 0,
    Liability = 1,
    Equity = 2,
    Income = 3,
    Expense = 4,
}
