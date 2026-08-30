namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices;

/// <summary>
/// Minimal manual-SaaS-billing lifecycle for a <see cref="SubscriptionInvoice"/> (ADR-034 §3) —
/// deliberately no accounting posting states (Draft/PartiallyPaid/etc. are not modeled here; this is
/// Platform Control Plane billing, not a ledger). All three terminal-from-<see cref="Issued"/>
/// transitions are mutually exclusive — see <see cref="SubscriptionInvoice"/>'s transition methods for
/// the exact guarded state machine.
/// </summary>
public enum SubscriptionInvoiceStatus
{
    Issued = 0,
    Paid = 1,
    Void = 2,
    Canceled = 3
}
