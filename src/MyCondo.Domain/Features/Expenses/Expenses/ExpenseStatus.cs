namespace MyCondo.Domain.Features.Expenses.Expenses;

/// <summary>
/// Formalized lifecycle (Template 3 — Expense Accounting Integration), superseding the pre-Template-3
/// MVP's <c>Recorded</c>/<c>Voided</c>-only pipeline. <c>Recorded</c> → <c>Posted</c> → (<c>Paid</c>,
/// unpaid expenses only) is a straight line; <c>Voided</c> is reachable from any non-terminal state and
/// is itself terminal. Approval and initial ledger posting happen together, in one action
/// (<c>ApproveExpenseCommand</c>) — see <see cref="Expense.MarkPosted"/> — rather than as separate
/// "approved but not yet posted" states the domain has no other use for.
/// </summary>
public enum ExpenseStatus
{
    /// <summary>Freely editable; no accounting consequence yet.</summary>
    Recorded = 0,
    /// <summary>Terminal. Reached either directly from <see cref="Recorded"/> (no ledger effect to
    /// reverse) or from <see cref="Posted"/>/<see cref="Paid"/> (reversing ledger postings are made
    /// first — see <see cref="Expense.Void"/>).</summary>
    Voided = 1,
    /// <summary>Approved and posted to the ledger — either "Dr Expense / Cr Accounts Payable" (unpaid)
    /// or "Dr Expense / Cr Bank/Cash" (immediate payment). Accounting values are now immutable.</summary>
    Posted = 2,
    /// <summary>Only reachable from <see cref="Posted"/> when the expense was recorded unpaid — a
    /// supplier payment ("Dr Accounts Payable / Cr Bank") has since been posted.</summary>
    Paid = 3,
}
