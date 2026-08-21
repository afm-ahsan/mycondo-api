using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Finance.Reports;

/// <summary>One account's activity within a Trial Balance window — <see cref="TotalDebit"/>/
/// <see cref="TotalCredit"/> are the raw sums of every <see cref="LedgerEntry"/> posted to this
/// account on or before the as-of date; the caller nets them against <see cref="NormalBalance"/> to
/// get a signed closing balance. Never stored — always derived from ledger entries.</summary>
public sealed record TrialBalanceAccountLine(
    ChartOfAccountId ChartOfAccountId,
    string Code,
    string Name,
    AccountCategory Category,
    LedgerDirection NormalBalance,
    decimal TotalDebit,
    decimal TotalCredit);

/// <summary>One journal line for General Ledger / Account Ledger browsing, flattened with its
/// posting and resolved account for display — the "Reporting-phase" cross-account journal browser
/// <c>FinanceEndpoints.cs</c> deferred at Template 1.</summary>
public sealed record LedgerActivityLine(
    LedgerEntryId EntryId,
    LedgerPostingId PostingId,
    DateOnly BusinessDate,
    string Description,
    string? ReferenceType,
    Guid? ReferenceId,
    ChartOfAccountId? ChartOfAccountId,
    string? ChartOfAccountCode,
    string? ChartOfAccountName,
    FlatId? FlatId,
    FundId? FundId,
    LedgerDirection Direction,
    decimal Amount);

/// <summary>One fund's raw activity within a window — same derive-don't-store rule as
/// <see cref="TrialBalanceAccountLine"/>. Funds are an optional <c>LedgerEntry</c> dimension no
/// posting role requires yet (see <c>Fund.cs</c>), so this can legitimately return an empty/zero
/// line for every configured fund until a posting flow starts tagging one.</summary>
public sealed record FundActivityLine(
    FundId FundId,
    string Code,
    string Name,
    decimal TotalDebit,
    decimal TotalCredit);

/// <summary>One Cash/Bank-touching entry paired with whether its posting's *other* leg(s) touch the
/// tenant's <c>FixedDeposit</c>-role account — the only Investing-activity account this system
/// models (no loan/owner-capital accounts exist, so there is no Financing activity to classify
/// either). Everything else (Resident receipts, expense payments, advances, interest, deposits held)
/// is Operating. When a posting's non-cash legs span more than one account (rare — e.g. an interest
/// receipt split between bank and a deduction-expense leg), <see cref="IsInvestingCounterLeg"/> is
/// true only if the single largest non-cash leg is the FixedDeposit account, since posting lines
/// can't be apportioned meaningfully across activities for a summary report.</summary>
public sealed record CashMovementLine(
    LedgerPostingId PostingId,
    DateOnly BusinessDate,
    LedgerDirection Direction,
    decimal Amount,
    string? ReferenceType,
    bool IsInvestingCounterLeg);

/// <summary>One income-account-type's Billed vs. Collected figures for a period — Template 5's
/// "permanently distinct figures" rule made concrete for Service Charge/Gas/Fine collection reporting.
/// <see cref="Billed"/> is the sum of <c>Invoice.TotalAmount</c> for invoices of the requested
/// <c>IncomeAccountType</c> issued in [FromDate, ToDate] (excluding Void). <see cref="Collected"/> is
/// the sum of <c>PaymentAllocation.AllocatedAmount</c> against those same-income-type invoices, from
/// Posted (non-reversed) payments with <c>BusinessDate</c> in [FromDate, ToDate] — independent of which
/// period the underlying invoice was billed in, since a payment can settle an older invoice.
/// <see cref="Waived"/> is the sum of <c>Invoice.WaivedAmount</c> for invoices of this income type
/// issued in the period (relevant mainly for Fine reporting — see <c>Invoice.Waive</c>'s "fine waiver"
/// example). Never a merged "revenue" figure — the three stay separate fields by construction.</summary>
public sealed record IncomeCollectionSummary(
    decimal Billed, int BilledInvoiceCount, decimal Collected, decimal Waived);

/// <summary>One calendar month's raw debit/credit activity on a single account — aggregated server-side
/// (SQL <c>GROUP BY</c> on year/month) for trend-style reports so the caller never pulls per-entry rows
/// into memory just to bucket them by month.</summary>
public sealed record MonthlyAccountActivityLine(
    int Year,
    int Month,
    decimal TotalDebit,
    decimal TotalCredit);

public interface IFinanceReportRepository
{
    /// <summary>Every account with at least one posted entry on or before <paramref name="asOfDate"/>,
    /// aggregated to raw debit/credit totals server-side.</summary>
    Task<IReadOnlyList<TrialBalanceAccountLine>> GetTrialBalanceAsync(
        Guid tenantId, DateOnly asOfDate, CancellationToken cancellationToken);

    Task<PagedResult<LedgerActivityLine>> GetGeneralLedgerAsync(
        Guid tenantId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? chartOfAccountId,
        Guid? fundId,
        string? referenceType,
        int page,
        int pageSize,
        bool ascending,
        CancellationToken cancellationToken);

    /// <summary>Sum(debits) - sum(credits) for one account strictly before <paramref name="asOfDate"/>
    /// (i.e. the opening balance for a period starting on <paramref name="asOfDate"/>) — raw, unsigned
    /// by <see cref="LedgerDirection"/>; the caller nets it against the account's normal balance.</summary>
    Task<(decimal TotalDebit, decimal TotalCredit)> GetAccountBalanceBeforeAsync(
        Guid tenantId, Guid chartOfAccountId, DateOnly asOfDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<FundActivityLine>> GetFundActivityAsync(
        Guid tenantId, DateOnly asOfDate, CancellationToken cancellationToken);

    /// <summary>The tenant's system Cash/Bank <see cref="ChartOfAccountId"/> plus every per-
    /// <c>FinancialAccount</c> child account created under it (Template 4) — the full set of accounts
    /// that represent physical cash/bank/MFS holdings, as opposed to other Asset-category accounts
    /// like <c>ResidentReceivable</c> or <c>FixedDeposit</c>.</summary>
    Task<IReadOnlyList<Guid>> GetCashAndBankChartOfAccountIdsAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CashMovementLine>> GetCashMovementsAsync(
        Guid tenantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>Same aggregation shape as <see cref="GetTrialBalanceAsync"/> but scoped to a
    /// [FromDate, ToDate] activity window (not cumulative-to-date) and filtered to accounts in
    /// <paramref name="category"/> — the Income &amp; Expense/Surplus-Deficit report needs period-flow
    /// totals, distinct from every other Accounting-group report's "as of" cumulative balances.</summary>
    Task<IReadOnlyList<TrialBalanceAccountLine>> GetCategoryActivityAsync(
        Guid tenantId, AccountCategory category, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>Billed/Collected/Waived for one tenant-wide income account type (ServiceChargeIncome,
    /// GasRecoveryIncome, or FineIncome) within a period — see <see cref="IncomeCollectionSummary"/>.
    /// </summary>
    Task<IncomeCollectionSummary> GetIncomeCollectionAsync(
        Guid tenantId, LedgerAccountType incomeAccountType, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>Resolves a <c>LedgerAccountType</c> posting role (by <c>AccountMapping.PostingRole</c>'s
    /// string key, e.g. <c>nameof(LedgerAccountType.OperatingExpense)</c>) to the tenant's actual
    /// <see cref="ChartOfAccountId"/> — null when the tenant has no mapping yet (same "no default account"
    /// posture <c>IFinancialPostingService</c> enforces). Generalizes the single-purpose lookups
    /// <see cref="GetCashAndBankChartOfAccountIdsAsync"/> already does inline for the CashOrBank role.
    /// </summary>
    Task<Guid?> GetChartOfAccountIdForPostingRoleAsync(Guid tenantId, string postingRole, CancellationToken cancellationToken);

    /// <summary>One account's debit/credit activity bucketed by calendar month across a date range —
    /// SQL-side <c>GROUP BY</c> on year/month, for trend-chart reports. See
    /// <see cref="MonthlyAccountActivityLine"/>.</summary>
    Task<IReadOnlyList<MonthlyAccountActivityLine>> GetAccountMonthlyActivityAsync(
        Guid tenantId, Guid chartOfAccountId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
}
