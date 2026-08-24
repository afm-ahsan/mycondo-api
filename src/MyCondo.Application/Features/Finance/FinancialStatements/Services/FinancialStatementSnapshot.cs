using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Services;

/// <summary>One account's signed contribution to a <see cref="FinancialStatementGroupAmount"/> —
/// <see cref="Amount"/> is already normalized to the account's <c>NormalBalance</c> direction (positive
/// = a balance/activity in the account's own normal direction), so callers never re-derive debit/credit
/// signs downstream.</summary>
public sealed record FinancialStatementAccountAmount(
    Guid ChartOfAccountId, string Code, string Name, decimal Amount);

/// <summary>One <see cref="FinancialStatementGroup"/> bucket's total plus the accounts that roll up
/// into it — the unit every future statement (Financial Position, Income &amp; Expenditure, Notes)
/// renders as a line/section.</summary>
public sealed record FinancialStatementGroupAmount(
    FinancialStatementGroup Group,
    AccountCategory Category,
    decimal Amount,
    IReadOnlyList<FinancialStatementAccountAmount> Accounts);

/// <summary>A tenant custom account with non-zero balance/activity that has no explicit
/// <c>ChartOfAccount.StatementGroup</c> assignment. It is still included in
/// <see cref="FinancialStatementSnapshot.Groups"/> under its <see cref="EffectiveStatementGroup"/>
/// fallback (Phase 2A plan §8/§15: unmapped accounts must never silently disappear) — this record is
/// purely the surfaced warning so a later statement/UI can flag it for classification.</summary>
public sealed record UnmappedAccountWarning(
    Guid ChartOfAccountId, string Code, string Name, AccountCategory Category,
    FinancialStatementGroup EffectiveStatementGroup, decimal Amount);

/// <summary>The shared aggregation result <see cref="IFinancialStatementReportingService"/> produces for
/// both an as-of-date snapshot (Statement of Financial Position) and a period snapshot (Income &amp;
/// Expenditure) — every future Financial Statements report (including Notes and comparisons) is built by
/// filtering/re-presenting this one shape rather than re-aggregating the ledger.</summary>
public sealed record FinancialStatementSnapshot(
    IReadOnlyList<FinancialStatementGroupAmount> Groups,
    IReadOnlyList<UnmappedAccountWarning> UnmappedAccountWarnings)
{
    /// <summary>Zero when the group has no non-zero activity — callers don't need a null check to
    /// render a statement line.</summary>
    public decimal AmountFor(FinancialStatementGroup group) =>
        Groups.FirstOrDefault(g => g.Group == group)?.Amount ?? 0m;

    /// <summary>Sum of every group belonging to <paramref name="category"/> — e.g. Total Assets, Total
    /// Income.</summary>
    public decimal TotalFor(AccountCategory category) =>
        Groups.Where(g => g.Category == category).Sum(g => g.Amount);
}
