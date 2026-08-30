using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices.Exceptions;

/// <summary>Guards <see cref="SubscriptionInvoice.ApplyPayment"/> against overpayment — no unapplied
/// credit/overpayment allocation exists in this task (ADR-034 Task 14C stop-gates), so a payment greater
/// than the invoice's current outstanding amount is rejected outright rather than partially applied.</summary>
public sealed class SubscriptionInvoicePaymentExceedsOutstandingException(
    SubscriptionInvoiceId id, decimal outstandingAmount, decimal paymentAmount)
    : DomainException(
        $"Payment amount {paymentAmount} exceeds subscription invoice {id}'s outstanding amount {outstandingAmount}.");
