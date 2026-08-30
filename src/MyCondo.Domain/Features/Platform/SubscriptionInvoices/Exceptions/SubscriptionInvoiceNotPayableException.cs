using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionInvoices.Exceptions;

/// <summary>Guards <see cref="SubscriptionInvoice.ApplyPayment"/> — only an <see cref="SubscriptionInvoiceStatus.Issued"/>
/// invoice can accept payment; Paid/Void/Canceled all reject it (ADR-034 Task 14C).</summary>
public sealed class SubscriptionInvoiceNotPayableException(SubscriptionInvoiceId id, SubscriptionInvoiceStatus status)
    : DomainException($"Subscription invoice {id} is {status} and cannot accept payment.");
