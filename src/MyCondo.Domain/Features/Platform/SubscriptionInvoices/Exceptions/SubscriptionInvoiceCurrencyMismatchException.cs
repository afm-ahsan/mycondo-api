using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices.Exceptions;

/// <summary>Guards <see cref="SubscriptionInvoice.ApplyPayment"/> — a payment's currency must match the
/// invoice's own <see cref="SubscriptionInvoice.Currency"/> exactly; no cross-currency conversion exists
/// in this task (ADR-034 Task 14C).</summary>
public sealed class SubscriptionInvoiceCurrencyMismatchException(
    SubscriptionInvoiceId id, string invoiceCurrency, string paymentCurrency)
    : DomainException(
        $"Payment currency '{paymentCurrency}' does not match subscription invoice {id}'s currency '{invoiceCurrency}'.");
