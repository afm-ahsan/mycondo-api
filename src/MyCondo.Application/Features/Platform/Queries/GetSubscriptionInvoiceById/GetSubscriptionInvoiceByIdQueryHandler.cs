using Mediator;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Queries.GetSubscriptionInvoiceById;

public sealed class GetSubscriptionInvoiceByIdQueryHandler(
    ISubscriptionInvoiceRepository subscriptionInvoices,
    ISubscriptionPaymentRepository subscriptionPayments,
    ITenantRepository tenants,
    IClock clock
) : IRequestHandler<GetSubscriptionInvoiceByIdQuery, PlatformSubscriptionInvoiceDetailDto>
{
    public async ValueTask<PlatformSubscriptionInvoiceDetailDto> Handle(
        GetSubscriptionInvoiceByIdQuery query, CancellationToken cancellationToken)
    {
        SubscriptionInvoiceId invoiceId = new(query.InvoiceId);
        SubscriptionInvoice invoice = await subscriptionInvoices.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(SubscriptionInvoice), query.InvoiceId);

        Tenant? tenant = await tenants.GetByIdAsync(invoice.TenantId, cancellationToken);

        IReadOnlyList<SubscriptionInvoiceLine> lines =
            await subscriptionInvoices.GetLinesAsync(invoiceId, cancellationToken);
        IReadOnlyList<SubscriptionPayment> payments =
            await subscriptionPayments.GetForInvoiceAsync(invoiceId, cancellationToken);

        DateOnly today = DateOnly.FromDateTime(DhakaTimeZone.ToLocal(clock.UtcNow).DateTime);

        return new PlatformSubscriptionInvoiceDetailDto(
            InvoiceId: invoice.Id.Value,
            TenantId: invoice.TenantId,
            OrganizationName: tenant?.Name ?? "Unknown",
            InvoiceNumber: invoice.InvoiceNumber,
            BillingPeriodStart: invoice.BillingPeriodStart,
            BillingPeriodEnd: invoice.BillingPeriodEnd,
            IssueDate: invoice.IssueDate,
            DueDate: invoice.DueDate,
            Currency: invoice.Currency,
            TotalAmount: invoice.TotalAmount,
            OutstandingAmount: invoice.OutstandingAmount,
            Status: invoice.Status.ToString(),
            DaysOverdue: SubscriptionInvoiceOverdueCalculator.DaysOverdue(invoice, today),
            IssuedAtUtc: invoice.IssuedAtUtc,
            PaidAtUtc: invoice.PaidAtUtc,
            VoidedAtUtc: invoice.VoidedAtUtc,
            VoidReason: invoice.VoidReason,
            CanceledAtUtc: invoice.CanceledAtUtc,
            CancelReason: invoice.CancelReason,
            Lines: lines
                .Select(l => new PlatformSubscriptionInvoiceLineDto(
                    l.Id.Value, l.PackageNameSnapshot, l.PackageVersionNumberSnapshot,
                    l.BillingCycleSnapshot.ToString(), l.BasePriceSnapshot, l.DiscountSnapshot, l.LineAmount,
                    l.Description))
                .ToList(),
            Payments: payments
                .Select(p => new PlatformSubscriptionPaymentDto(
                    p.Id.Value, p.Amount, p.Currency, p.PaymentDate, p.ReferenceNumber, p.Notes, p.RecordedAtUtc))
                .ToList());
    }
}
