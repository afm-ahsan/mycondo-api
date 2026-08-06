using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Billing.Invoices.Exceptions;

public sealed class InvoiceAlreadyVoidException(InvoiceId invoiceId)
    : DomainException($"Invoice {invoiceId} is already void.")
{
    public InvoiceId InvoiceId { get; } = invoiceId;
}
