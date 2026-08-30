using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.RecordSubscriptionPayment;

/// <summary>
/// Manually records one payment against a <see cref="Domain.Features.Platform.SubscriptionInvoices.SubscriptionInvoice"/>
/// (ADR-034 Task 14C). <see cref="PaymentDate"/> is caller-supplied (a Bangladesh business date) rather
/// than derived from "now" — manual recording is expected to happen after the fact (e.g. a bank
/// transfer settled days earlier), the same way <see cref="Domain.Features.Platform.SubscriptionInvoices.SubscriptionInvoice"/>'s
/// own <c>BillingPeriodStart</c> is caller-supplied in Task 14B. <see cref="ReferenceNumber"/> is
/// mandatory — it is this command's idempotency key.
/// </summary>
public sealed record RecordSubscriptionPaymentCommand(
    Guid OrganizationId,
    Guid SubscriptionInvoiceId,
    decimal Amount,
    string Currency,
    DateOnly PaymentDate,
    string ReferenceNumber,
    string? Notes
) : IRequest<Guid>;
