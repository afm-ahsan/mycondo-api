using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Notes;

/// <summary>Stable key identifying one supporting schedule. Deliberately <em>not</em> a "Note 1/2/3"
/// ordinal: presentation numbering is a rendering concern (Task 6/8) that changes whenever a tenant has
/// no data for a schedule, whereas this key is the durable contract a client filters and drills down on.
/// Never renumber or remove a shipped member.</summary>
public enum FinancialStatementNoteKey
{
    CashAndBank = 0,
    FixedDeposits = 1,
    ServiceChargeReceivable = 2,
    ResidentAdvances = 3,
    Payables = 4,
    InterestIncome = 5,
    OperatingExpensesByCategory = 6,
}

/// <summary>Whether a note explains a point-in-time balance (Statement of Financial Position) or a
/// window of activity (Income &amp; Expenditure) — the two date semantics of Phase 2A's statements.</summary>
public enum FinancialStatementNoteScope
{
    AsOfDate = 0,
    Period = 1,
}

/// <summary>One Cash/Bank holding. <see cref="Balance"/> is reconstructed from posted ledger entries
/// through the reporting date — never <c>FinancialAccount</c>'s current state, which would misstate a
/// historical statement. <see cref="IsUnlinkedGlAccount"/> marks a general-ledger account classified
/// into Cash &amp; Bank with no owning <c>FinancialAccount</c> (typically the tenant's default system
/// <c>CashOrBank</c> account, which pre-Template-4 postings still resolve to); such a row is still a
/// genuine cash balance, so it counts toward the schedule total rather than being treated as
/// unexplained. <see cref="MaskedAccountNumber"/> shows at most the trailing four characters.</summary>
public sealed record CashAndBankNoteRow(
    Guid? FinancialAccountId,
    Guid ChartOfAccountId,
    string AccountCode,
    string Name,
    FinancialAccountType? AccountType,
    string? BankName,
    string? BranchName,
    string? MaskedAccountNumber,
    Guid? FundId,
    bool? IsActive,
    decimal Balance,
    bool IsUnlinkedGlAccount);

/// <summary>One Fixed Deposit's principal position. <see cref="CarryingBalance"/> is GL-derived as of
/// the reporting date; <see cref="CurrentRecordedPrincipal"/> and <see cref="CurrentStatus"/> are the
/// instrument's <em>present</em> values and are labelled as such because the aggregate stores no history
/// of either — they are context, never a substitute for the as-of-date figure.
/// <see cref="AccruedInterestReceivable"/> is a separate GL balance (the Accrued Interest Receivable
/// group) reported alongside but never folded into the principal total, per Phase 2A's requirement to
/// keep principal, accrued interest receivable and interest income distinct.</summary>
public sealed record FixedDepositNoteRow(
    Guid? FixedDepositId,
    string? CertificateNumber,
    string? BankName,
    string? BranchName,
    DateOnly? StartDate,
    DateOnly? MaturityDate,
    decimal? CurrentRecordedPrincipal,
    decimal? InterestRatePercent,
    FixedDepositStatus? CurrentStatus,
    Guid? FundId,
    decimal CarryingBalance,
    decimal AccruedInterestReceivable,
    bool IsUnattributed,
    string? UnattributedReason);

/// <summary>One flat's receivable or advance balance, reconstructed from the flat-scoped ledger
/// entries the posting engine already stamps with a <c>FlatId</c>. Carries no resident identity — see
/// <c>NoteFlatRef</c> for why.</summary>
public sealed record FlatBalanceNoteRow(
    Guid? FlatId,
    string? FlatNumber,
    Guid? BuildingId,
    string? BuildingName,
    decimal Balance,
    bool IsUnattributed);

/// <summary>One expense's still-outstanding payable. <see cref="OutstandingAmount"/> is the GL-derived
/// net of every Accounts Payable movement referencing this expense through the reporting date, so an
/// expense already paid (or voided) nets to zero and drops out — the schedule shows liabilities, not the
/// expense register. <see cref="OriginalAmount"/>/<see cref="Status"/> are the expense record's current
/// values, shown as context.</summary>
public sealed record PayableNoteRow(
    Guid? ExpenseId,
    string? Description,
    string? Payee,
    string? ReferenceNumber,
    Guid? ExpenseCategoryId,
    string? ExpenseCategoryName,
    string? ExpenseTypeName,
    DateOnly? AccountingDate,
    decimal? OriginalAmount,
    ExpenseStatus? Status,
    decimal OutstandingAmount,
    bool IsUnattributed,
    string? UnattributedReason);

/// <summary>One Fixed Deposit's interest activity for the period. <see cref="InterestRecognized"/> is
/// the GL figure the Income &amp; Expenditure statement reports and the only one this schedule
/// reconciles; <see cref="InterestReceivedGross"/>/<see cref="InterestDeducted"/> come from the FD
/// interest-receipt subledger and are deliberately separate — interest is recognized at accrual and
/// received later, so the two figures are not expected to match in any given period.</summary>
public sealed record InterestIncomeNoteRow(
    Guid? FixedDepositId,
    string? CertificateNumber,
    string? BankName,
    decimal InterestRecognized,
    decimal InterestReceivedGross,
    decimal InterestDeducted,
    bool IsUnattributed,
    string? UnattributedReason);

/// <summary>One <c>ExpenseCategory</c>'s share of the single General-Ledger Operating Expense total.
/// The breakdown exists because operating-expense categories are tenant-configurable data, not chart-of-
/// accounts entries (ADR-030) — this row explains the GL figure, it does not replace it.
/// <see cref="ExpenseCategoryId"/> is null for expenses whose <c>ExpenseType</c> predates categories,
/// and for the unattributed bucket.</summary>
public sealed record OperatingExpenseCategoryNoteRow(
    Guid? ExpenseCategoryId,
    string CategoryName,
    int ExpenseCount,
    decimal Amount,
    bool IsUnattributed);

/// <summary>
/// One supporting schedule ("Note to the Accounts") explaining a Financial Statement figure.
///
/// <see cref="GlBalance"/> is always the authoritative General Ledger number — the same figure the
/// primary statement reports for <see cref="StatementGroup"/>, computed from the same posted ledger
/// entries with the same date and fund filters. <see cref="ScheduleBalance"/> is what the schedule's
/// rows account for. <see cref="Difference"/> is returned unconditionally and is never suppressed,
/// rounded away, or "corrected" by adjusting either side: a non-zero difference means ledger activity
/// exists that this schedule could not trace to a subledger record (see
/// <see cref="UnattributedAmount"/> and <see cref="Warnings"/>), and the primary statement's figure
/// remains the reported one regardless.
/// </summary>
public sealed record FinancialStatementNote(
    FinancialStatementNoteKey Key,
    string Title,
    FinancialStatementNoteScope Scope,
    FinancialStatementGroup StatementGroup,
    DateOnly? StartDate,
    DateOnly EndDate,
    Guid? FundId,
    decimal GlBalance,
    decimal ScheduleBalance,
    decimal Difference,
    bool IsReconciled,
    decimal UnattributedAmount,
    int TotalRowCount,
    bool IsDetailTruncated,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<CashAndBankNoteRow>? CashAndBankRows = null,
    IReadOnlyList<FixedDepositNoteRow>? FixedDepositRows = null,
    IReadOnlyList<FlatBalanceNoteRow>? FlatBalanceRows = null,
    IReadOnlyList<PayableNoteRow>? PayableRows = null,
    IReadOnlyList<InterestIncomeNoteRow>? InterestIncomeRows = null,
    IReadOnlyList<OperatingExpenseCategoryNoteRow>? OperatingExpenseCategoryRows = null);
