namespace MyCondo.Domain.Features.Expenses.Expenses;

/// <summary>
/// MVP lifecycle: <see cref="Recorded"/> → <see cref="Voided"/> only — deliberately not a
/// Draft/Submitted/Approved/Paid approval pipeline. The permission catalogue only defines
/// <c>expense.view</c>/<c>expense.manage</c> (no <c>expense.approve</c>/<c>expense.pay</c>), and every
/// MVP requirement this satisfies (record an expense safely, correct a mistake without destroying the
/// audit trail) is met by a single "who can write" permission plus a void-with-reason escape hatch —
/// see mycondo-docs expense-management domain decision for the full rationale and the deferred richer
/// workflow this leaves room to add later without a breaking migration.
/// </summary>
public enum ExpenseStatus
{
    Recorded = 0,
    Voided = 1,
}
