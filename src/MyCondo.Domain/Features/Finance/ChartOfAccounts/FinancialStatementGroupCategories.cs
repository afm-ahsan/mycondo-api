namespace MyCondo.Domain.Features.Finance.ChartOfAccounts;

/// <summary>
/// Declares which <see cref="AccountCategory"/> each <see cref="FinancialStatementGroup"/> belongs to,
/// and the fallback group a category resolves to when a <see cref="ChartOfAccount"/> has no explicit
/// <see cref="ChartOfAccount.StatementGroup"/> — guarantees every account, including a tenant custom
/// account created without an explicit classification, still rolls up into a Financial Statement rather
/// than silently disappearing (Phase 2A plan §8/§15).
/// </summary>
public static class FinancialStatementGroupCategories
{
    private static readonly Dictionary<FinancialStatementGroup, AccountCategory> CategoryByGroup =
        new Dictionary<FinancialStatementGroup, AccountCategory>
        {
            [FinancialStatementGroup.CashAndBank] = AccountCategory.Asset,
            [FinancialStatementGroup.InvestmentsAndFixedDeposits] = AccountCategory.Asset,
            [FinancialStatementGroup.Receivables] = AccountCategory.Asset,
            [FinancialStatementGroup.AccruedInterestReceivable] = AccountCategory.Asset,
            [FinancialStatementGroup.OtherAssets] = AccountCategory.Asset,

            [FinancialStatementGroup.AccountsPayable] = AccountCategory.Liability,
            [FinancialStatementGroup.ResidentAdvances] = AccountCategory.Liability,
            [FinancialStatementGroup.OtherLiabilities] = AccountCategory.Liability,

            [FinancialStatementGroup.FundBalance] = AccountCategory.Equity,
            [FinancialStatementGroup.AccumulatedSurplus] = AccountCategory.Equity,

            [FinancialStatementGroup.ServiceChargeIncome] = AccountCategory.Income,
            [FinancialStatementGroup.UtilityGasIncome] = AccountCategory.Income,
            [FinancialStatementGroup.FineIncome] = AccountCategory.Income,
            [FinancialStatementGroup.InterestIncome] = AccountCategory.Income,
            [FinancialStatementGroup.OtherIncome] = AccountCategory.Income,

            [FinancialStatementGroup.OperatingExpenses] = AccountCategory.Expense,
            [FinancialStatementGroup.InterestExpense] = AccountCategory.Expense,
            [FinancialStatementGroup.OtherExpenses] = AccountCategory.Expense,
        };

    private static readonly Dictionary<AccountCategory, FinancialStatementGroup> DefaultGroupByCategory =
        new Dictionary<AccountCategory, FinancialStatementGroup>
        {
            [AccountCategory.Asset] = FinancialStatementGroup.OtherAssets,
            [AccountCategory.Liability] = FinancialStatementGroup.OtherLiabilities,
            [AccountCategory.Equity] = FinancialStatementGroup.AccumulatedSurplus,
            [AccountCategory.Income] = FinancialStatementGroup.OtherIncome,
            [AccountCategory.Expense] = FinancialStatementGroup.OtherExpenses,
        };

    /// <summary>The single <see cref="AccountCategory"/> a statement group belongs to.</summary>
    public static AccountCategory CategoryOf(FinancialStatementGroup group) => CategoryByGroup[group];

    /// <summary>The fallback group used when a <see cref="ChartOfAccount"/> of this category has no
    /// explicit <see cref="ChartOfAccount.StatementGroup"/> assigned.</summary>
    public static FinancialStatementGroup DefaultFor(AccountCategory category) => DefaultGroupByCategory[category];

    /// <summary>True when <paramref name="group"/> is a valid classification for an account of
    /// <paramref name="category"/>.</summary>
    public static bool IsValidFor(FinancialStatementGroup group, AccountCategory category) =>
        CategoryOf(group) == category;
}
