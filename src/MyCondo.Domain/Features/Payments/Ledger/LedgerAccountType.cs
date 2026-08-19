namespace MyCondo.Domain.Features.Payments.Ledger;

/// <summary>
/// A deliberately minimal chart of accounts — just enough for genuine double-entry postings across
/// the registers this platform digitizes (resident charges, payments, waivers, migrated opening
/// balances), not a full general ledger (no expense/payroll/bank-reconciliation accounts — those stay
/// out of scope per the register digitization spec §15).
/// </summary>
public enum LedgerAccountType
{
    /// <summary>Per-flat "money owed to the association" — the only account type that requires a
    /// <see cref="LedgerEntry.FlatId"/>; every other account type is tenant-wide.</summary>
    ResidentReceivable = 0,
    AssociationRevenue = 1,
    CashOrBank = 2,
    AdjustmentsAndWaivers = 3,
    /// <summary>Used only when recording a migrated legacy opening balance — crediting
    /// AssociationRevenue for a balance that isn't new revenue would misstate income.</summary>
    OpeningBalanceEquity = 4,
    /// <summary>Added in Slice G — a facility-booking refundable security deposit, held as a liability
    /// from collection until settlement (full refund, partial refund with deduction, or forfeiture).
    /// Tenant-wide like every non-<see cref="ResidentReceivable"/>/<see cref="ResidentAdvance"/>
    /// account; deposit lines carry no <see cref="LedgerEntry.FlatId"/> even though the deposit is
    /// conceptually flat-specific, per <see cref="LedgerPosting.Create"/>'s existing invariant.</summary>
    RefundableDepositsHeld = 5,
    /// <summary>Added by ADR-027's Billing↔Finance integration — Service Charge revenue, kept
    /// separate from <see cref="AssociationRevenue"/> so Service Charge income is independently
    /// reconcilable. The receivable side stays unified on <see cref="ResidentReceivable"/> (see that
    /// member's doc comment); only the income/credit side is differentiated by charge type.</summary>
    ServiceChargeIncome = 6,
    /// <summary>Gas utility recovery income (Utility invoices where <c>UtilityType == Gas</c>).
    /// Other utility types (e.g. Electricity) continue posting to <see cref="AssociationRevenue"/> —
    /// only Gas is differentiated, per the Billing↔Finance integration's explicit scope.</summary>
    GasRecoveryIncome = 7,
    /// <summary>Fine/penalty income, distinct from <see cref="AssociationRevenue"/> so fines remain
    /// independently traceable/reconcilable.</summary>
    FineIncome = 8,
    /// <summary>Per-flat "money the association owes back to a resident" — a payment overpayment that
    /// exceeds every outstanding receivable becomes unallocated credit here instead of the flat's
    /// <see cref="ResidentReceivable"/> balance going negative. Like <see cref="ResidentReceivable"/>,
    /// this requires a <see cref="LedgerEntry.FlatId"/> — see <see cref="LedgerPosting.Create"/>.
    /// </summary>
    ResidentAdvance = 9,
}
