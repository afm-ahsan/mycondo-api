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
    /// Tenant-wide like every non-<see cref="ResidentReceivable"/> account; deposit lines carry no
    /// <see cref="LedgerEntry.FlatId"/> even though the deposit is conceptually flat-specific, per
    /// <see cref="LedgerPosting.Create"/>'s existing, unmodified invariant.</summary>
    RefundableDepositsHeld = 5,
}
