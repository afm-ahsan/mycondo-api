using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Billing.Invoices.Exceptions;

public sealed class InvoiceAlreadyWaivedException(InvoiceId invoiceId)
    : DomainException($"Invoice {invoiceId} has already had a waiver applied.")
{
    public InvoiceId InvoiceId { get; } = invoiceId;
}
