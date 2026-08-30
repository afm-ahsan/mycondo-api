using Mediator;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Queries.GetSubscriptionInvoicePayments;

/// <summary>Platform-scope payment history for one invoice (ADR-034 Task 14D) — display only; the
/// invoice's own persisted OutstandingAmount remains the balance authority.</summary>
public sealed record GetSubscriptionInvoicePaymentsQuery(Guid InvoiceId)
    : IRequest<IReadOnlyList<PlatformSubscriptionPaymentDto>>;
