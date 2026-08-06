namespace MyCondo.Domain.Features.Billing.Invoices;

public enum InvoiceStatus
{
    Issued = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Void = 3,
}
