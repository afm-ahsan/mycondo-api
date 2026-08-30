using Mediator;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;

namespace MyCondo.Application.Features.Platform.Queries.GetSubscriptionInvoicePayments;

public sealed class GetSubscriptionInvoicePaymentsQueryHandler(
    ISubscriptionInvoiceRepository subscriptionInvoices,
    ISubscriptionPaymentRepository subscriptionPayments
) : IRequestHandler<GetSubscriptionInvoicePaymentsQuery, IReadOnlyList<PlatformSubscriptionPaymentDto>>
{
    public async ValueTask<IReadOnlyList<PlatformSubscriptionPaymentDto>> Handle(
        GetSubscriptionInvoicePaymentsQuery query, CancellationToken cancellationToken)
    {
        SubscriptionInvoiceId invoiceId = new(query.InvoiceId);
        SubscriptionInvoice invoice = await subscriptionInvoices.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(SubscriptionInvoice), query.InvoiceId);

        IReadOnlyList<SubscriptionPayment> payments =
            await subscriptionPayments.GetForInvoiceAsync(invoice.Id, cancellationToken);

        return payments
            .Select(p => new PlatformSubscriptionPaymentDto(
                p.Id.Value, p.Amount, p.Currency, p.PaymentDate, p.ReferenceNumber, p.Notes, p.RecordedAtUtc))
            .ToList();
    }
}
