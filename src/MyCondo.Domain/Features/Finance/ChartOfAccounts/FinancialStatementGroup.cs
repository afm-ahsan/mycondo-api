namespace MyCondo.Domain.Features.Finance.ChartOfAccounts;

/// <summary>
/// Organization-agnostic reporting bucket a <see cref="ChartOfAccount"/> rolls up into for the
/// Financial Statements feature (CondoBD Finance Phase 2A, Task 1) — the Statement of Financial
/// Position and the top level of the Income &amp; Expenditure Statement. Each member belongs to exactly
/// one <see cref="AccountCategory"/> — see <see cref="FinancialStatementGroupCategories"/>.
///
/// Deliberately coarser than the Phase 2A plan's illustrative Expenditure breakdown:
/// <see cref="OperatingExpenses"/> is a single group because every non-FD-interest expense posts
/// through one tenant-wide system account (<c>LedgerAccountType.OperatingExpense</c>, ADR-027/Template
/// 3). The finer categories the plan shows (Maintenance, Utilities, Gas Purchase, Security, Staff,
/// Administration, Repairs, Community, Miscellaneous) are tenant-configurable <c>ExpenseCategory</c>
/// data, not a chart-of-accounts concept — that breakdown belongs to the Income &amp; Expenditure
/// supporting schedule (Task 4/5), reconciled back to this group's General Ledger total, not
/// reintroduced here as parallel GL accounts.
///
/// Similarly, <see cref="ServiceChargeIncome"/> covers both regular and additional service-charge
/// invoices — <c>GenerateInvoiceBatchCommandHandler</c> posts every service-charge invoice type to the
/// single <c>ServiceChargeIncome</c> ledger role, so the two are not independently reconcilable at the
/// GL level today.
///
/// Never remove or renumber a shipped member — persisted via <c>HasConversion&lt;string&gt;</c>
/// (the enum name, not its numeric value, is what's stored).
/// </summary>
public enum FinancialStatementGroup
{
    // Assets
    CashAndBank = 0,
    InvestmentsAndFixedDeposits = 1,
    Receivables = 2,
    AccruedInterestReceivable = 3,
    OtherAssets = 4,

    // Liabilities
    AccountsPayable = 10,
    ResidentAdvances = 11,
    OtherLiabilities = 12,

    // Funds / Equity
    FundBalance = 20,
    AccumulatedSurplus = 21,

    // Income
    ServiceChargeIncome = 30,
    UtilityGasIncome = 31,
    FineIncome = 32,
    InterestIncome = 33,
    OtherIncome = 34,

    // Expenditure
    OperatingExpenses = 40,
    InterestExpense = 41,
    OtherExpenses = 42,
}
