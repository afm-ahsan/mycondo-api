using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices.Exceptions;

/// <summary>Guards <see cref="SubscriptionInvoice"/>'s lifecycle state machine (ADR-034 §3) — thrown
/// for any transition attempt other than Issued→Paid, Issued→Void, or Issued→Canceled.</summary>
public sealed class SubscriptionInvoiceInvalidTransitionException(
    SubscriptionInvoiceId id, SubscriptionInvoiceStatus from, SubscriptionInvoiceStatus to)
    : DomainException($"Subscription invoice {id} cannot transition from {from} to {to}.");
