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
}
