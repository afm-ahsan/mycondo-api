using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.GenerateSubscriptionInvoice;

/// <summary>
/// Manually generates one <see cref="Domain.Features.Platform.SubscriptionInvoices.SubscriptionInvoice"/>
/// for an organization's current subscription (ADR-034 Task 14B). <see cref="BillingPeriodStart"/> is the
/// only period input the caller supplies — the end date is derived deterministically from the
/// subscription's own BillingCycle by the handler, so a period can never be requested that doesn't match
/// what the subscription is actually billed for.
/// </summary>
public sealed record GenerateSubscriptionInvoiceCommand(
    Guid OrganizationId,
    DateOnly BillingPeriodStart
) : IRequest<Guid>;
