using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Finance.Reports;

/// <summary>One general-ledger account's raw debit/credit totals within a note window — the
/// per-account decomposition a Cash &amp; Bank schedule needs. Same derive-don't-store rule as
/// <see cref="TrialBalanceAccountLine"/>: never a stored balance, always summed from posted
/// <c>LedgerEntry</c> rows, so a historical as-of date is reconstructed rather than approximated by
/// today's balance.</summary>
public sealed record NoteAccountBalanceLine(
    ChartOfAccountId ChartOfAccountId, decimal TotalDebit, decimal TotalCredit);

/// <summary>One flat's raw debit/credit totals within a note window on a given set of accounts — the
/// per-flat decomposition the Service Charge Receivable and Resident Advance schedules need.
/// <see cref="FlatId"/> is null for ledger activity on those accounts that carries no flat attribution
/// (which <c>LedgerPosting.Create</c>'s flat-scoped-account invariant should prevent, but the schedule
/// surfaces rather than discards).</summary>
public sealed record NoteFlatBalanceLine(FlatId? FlatId, decimal TotalDebit, decimal TotalCredit);

/// <summary>One <c>LedgerPosting.ReferenceType</c>/<c>ReferenceId</c> pair's raw debit/credit totals
/// within a note window on a given set of accounts — the subledger-source decomposition the Fixed
/// Deposit, Payables, Interest Income and Operating Expense schedules need, since those subledgers have
/// no dedicated <c>LedgerEntry</c> dimension (unlike <c>FlatId</c>) and are reachable only through the
/// posting's reference (ADR-027).</summary>
public sealed record NotePostingReferenceLine(
    string? ReferenceType, Guid? ReferenceId, decimal TotalDebit, decimal TotalCredit);

/// <summary>A posting's own reference pair — needed because reversal/void postings reference the
/// <em>original posting id</em> rather than the business record (see <c>VoidExpenseCommandHandler</c>),
/// so attributing a reversal to its subledger record takes a second hop through this lookup.</summary>
public sealed record NotePostingReference(LedgerPostingId PostingId, string? ReferenceType, Guid? ReferenceId);

/// <summary>Display/drill-down metadata for one <see cref="FinancialAccount"/>, keyed by the account's
/// own <see cref="ChartOfAccountId"/> so a GL-derived Cash &amp; Bank row can be labelled without a
/// per-row query. <see cref="AccountNumber"/> is returned raw and masked by the Application layer —
/// the repository does not decide presentation.</summary>
public sealed record NoteFinancialAccountRef(
    FinancialAccountId FinancialAccountId,
    ChartOfAccountId ChartOfAccountId,
    string Name,
    FinancialAccountType AccountType,
    string? BankName,
    string? BranchName,
    string? AccountNumber,
    FundId? FundId,
    bool IsActive);

/// <summary>Display/drill-down metadata for one <see cref="FixedDeposit"/>. <see cref="Status"/> and
/// <see cref="Principal"/> are the instrument's <em>current</em> values — the schedule reports them as
/// such and never substitutes them for the GL-derived as-of-date carrying balance.</summary>
public sealed record NoteFixedDepositRef(
    FixedDepositId FixedDepositId,
    string CertificateNumber,
    string BankName,
    string? BranchName,
    DateOnly StartDate,
    DateOnly MaturityDate,
    decimal Principal,
    decimal InterestRatePercent,
    FixedDepositStatus Status,
    FundId? FundId);

/// <summary>Maps an FD interest accrual/receipt id (the <c>SourceId</c> those postings actually carry)
/// back to its owning <see cref="FixedDepositId"/>, plus the receipt's gross/deduction split — FD
/// interest postings reference the accrual/receipt, not the FD, so per-FD interest attribution needs
/// this hop.</summary>
public sealed record NoteFixedDepositInterestSourceRef(
    Guid SourceId, FixedDepositId FixedDepositId, decimal GrossAmount, decimal DeductionAmount, bool IsReceipt);

/// <summary>Display/drill-down metadata for one <see cref="Expense"/>, already joined to its
/// <c>ExpenseType</c>'s <see cref="ExpenseCategory"/> so the Operating Expense schedule groups by
/// category without an N+1 lookup. <see cref="ExpenseCategoryId"/> is null for a legacy
/// <c>ExpenseType</c> created before Template 3 introduced categories.</summary>
public sealed record NoteExpenseRef(
    ExpenseId ExpenseId,
    string Description,
    string? Payee,
    string? ReferenceNumber,
    DateOnly AccountingDate,
    decimal Amount,
    ExpenseStatus Status,
    ExpenseCategoryId? ExpenseCategoryId,
    string? ExpenseCategoryName,
    string ExpenseTypeName);

/// <summary>Display metadata for one flat, including its building's name — the label a per-flat
/// receivable/advance row renders. Deliberately carries no resident identity: who was responsible for a
/// flat on an arbitrary historical date is a time-varying ownership/occupancy question this schedule
/// does not attempt to reconstruct (and would expose personal data the GL itself never records).</summary>
public sealed record NoteFlatRef(FlatId FlatId, string FlatNumber, Guid BuildingId, string BuildingName);

/// <summary>
/// The subledger-decomposition queries behind CondoBD Finance Phase 2A Task 5's "Notes to Accounts"
/// supporting schedules. Every balance-producing method here aggregates the <em>same posted
/// <c>LedgerEntry</c> rows</em> the primary statements are built from — restricted to a caller-supplied
/// set of <c>ChartOfAccountId</c>s and grouped by a business dimension — so a schedule explains a
/// General Ledger figure rather than replacing it with an independently computed subledger total
/// (Phase 2A plan §13). The remaining methods return only labelling/drill-down metadata.
///
/// Kept separate from <see cref="IFinanceReportRepository"/> (which owns the statement-level
/// aggregations of Tasks 2-4) because Notes are a distinct read model with its own row shapes; the two
/// are composed by the Application layer, never by one calling the other.
/// </summary>
public interface IFinancialStatementNoteRepository
{
    /// <summary>Per-account debit/credit totals across <paramref name="chartOfAccountIds"/>. When
    /// <paramref name="fromDate"/> is null the window is cumulative through <paramref name="toDate"/>
    /// (as-of semantics, for a Statement of Financial Position note); when supplied it is
    /// [fromDate, toDate] inclusive (period semantics, for an Income &amp; Expenditure note).</summary>
    Task<IReadOnlyList<NoteAccountBalanceLine>> GetNoteAccountBalancesAsync(
        Guid tenantId, IReadOnlyList<Guid> chartOfAccountIds, DateOnly? fromDate, DateOnly toDate,
        Guid? fundId, CancellationToken cancellationToken);

    /// <summary>Per-flat debit/credit totals across <paramref name="chartOfAccountIds"/> — same window
    /// semantics as <see cref="GetNoteAccountBalancesAsync"/>.</summary>
    Task<IReadOnlyList<NoteFlatBalanceLine>> GetNoteFlatBalancesAsync(
        Guid tenantId, IReadOnlyList<Guid> chartOfAccountIds, DateOnly? fromDate, DateOnly toDate,
        Guid? fundId, CancellationToken cancellationToken);

    /// <summary>Per-posting-reference debit/credit totals across <paramref name="chartOfAccountIds"/> —
    /// same window semantics as <see cref="GetNoteAccountBalancesAsync"/>.</summary>
    Task<IReadOnlyList<NotePostingReferenceLine>> GetNotePostingReferenceBalancesAsync(
        Guid tenantId, IReadOnlyList<Guid> chartOfAccountIds, DateOnly? fromDate, DateOnly toDate,
        Guid? fundId, CancellationToken cancellationToken);

    /// <summary>The <c>ReferenceType</c>/<c>ReferenceId</c> of each requested posting — the second hop
    /// described on <see cref="NotePostingReference"/>. Bounded by the caller to the reversal references
    /// actually seen in the window.</summary>
    Task<IReadOnlyList<NotePostingReference>> GetPostingReferencesAsync(
        Guid tenantId, IReadOnlyList<Guid> postingIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<NoteFinancialAccountRef>> GetFinancialAccountRefsAsync(
        Guid tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NoteFixedDepositRef>> GetFixedDepositRefsAsync(
        Guid tenantId, IReadOnlyList<Guid> fixedDepositIds, CancellationToken cancellationToken);

    /// <summary>Resolves FD interest accrual and receipt ids to their owning fixed deposit. Receipt rows
    /// additionally carry the gross/deduction split so the Interest Income schedule can report "interest
    /// received" separately from "interest recognized as income".</summary>
    Task<IReadOnlyList<NoteFixedDepositInterestSourceRef>> GetFixedDepositInterestSourceRefsAsync(
        Guid tenantId, IReadOnlyList<Guid> sourceIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<NoteExpenseRef>> GetExpenseRefsAsync(
        Guid tenantId, IReadOnlyList<Guid> expenseIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<NoteFlatRef>> GetFlatRefsAsync(
        Guid tenantId, IReadOnlyList<Guid> flatIds, CancellationToken cancellationToken);

    /// <summary>Every chart-of-accounts row for the tenant, reduced to what note classification needs.
    /// The set of accounts belonging to a <see cref="FinancialStatementGroup"/> cannot be filtered in
    /// SQL — <c>ChartOfAccount.EffectiveStatementGroup</c> is a computed property (<c>Ignore</c>d by the
    /// EF configuration) whose fallback depends on <see cref="AccountCategory"/> — so the (small,
    /// per-tenant) account list is resolved once and filtered in memory, exactly as
    /// <c>FinanceReportRepository</c> already does when projecting statement lines.</summary>
    Task<IReadOnlyList<ChartOfAccount>> GetChartOfAccountsAsync(Guid tenantId, CancellationToken cancellationToken);
}
