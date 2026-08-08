using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Mappings;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.PaymentAllocations;
using MyCondo.Domain.Features.Payments.Payments;

namespace MyCondo.Application.Features.Payments.Queries.GetPaymentById;

public sealed class GetPaymentByIdQueryHandler(
    IPaymentRepository payments,
    IPaymentAllocationRepository paymentAllocations,
    IInvoiceRepository invoices,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetPaymentByIdQuery, PaymentDto>
{
    public async ValueTask<PaymentDto> Handle(GetPaymentByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        PaymentId id = new(query.PaymentId);
        Payment payment = await payments.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), query.PaymentId);
        if (payment.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Payment), query.PaymentId);
        }

        IReadOnlyList<PaymentAllocation> allocations = await paymentAllocations.GetForPaymentAsync(id, cancellationToken);

        Dictionary<InvoiceId, string> invoiceNumbersById = [];
        List<(PaymentAllocation Allocation, string InvoiceNumber)> allocationsWithInvoiceNumbers = [];
        foreach (PaymentAllocation allocation in allocations)
        {
            if (!invoiceNumbersById.TryGetValue(allocation.InvoiceId, out string? invoiceNumber))
            {
                Invoice invoice = await invoices.GetByIdAsync(allocation.InvoiceId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Invoice {allocation.InvoiceId} referenced by allocation {allocation.Id} not found.");
                invoiceNumber = invoice.InvoiceNumber;
                invoiceNumbersById[allocation.InvoiceId] = invoiceNumber;
            }

            allocationsWithInvoiceNumbers.Add((allocation, invoiceNumber));
        }

        return payment.ToDto(allocationsWithInvoiceNumbers);
    }
}
