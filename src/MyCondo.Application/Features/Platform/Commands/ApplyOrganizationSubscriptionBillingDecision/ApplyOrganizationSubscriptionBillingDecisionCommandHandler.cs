using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Services.BillingLifecycleDecision;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.ApplyOrganizationSubscriptionBillingDecision;

/// <summary>
/// Task 14F server-authoritative apply action. Consumes the Task 14E <see cref="IBillingLifecycleDecisionService"/>
/// exclusively — it never independently recalculates the billing lifecycle decision matrix. The
/// subscription and its outstanding invoices are both loaded fresh inside this handler and the
/// recommendation is evaluated and applied to that same in-memory aggregate before returning, so there
/// is no window in which a stale client-supplied recommendation, status, or days-overdue could be
/// trusted — none of those are even accepted as input. Applies at most one existing guarded lifecycle
/// transition (never more than one per invocation) and never assigns <see cref="OrganizationSubscription.Status"/>
/// directly.
/// </summary>
public sealed class ApplyOrganizationSubscriptionBillingDecisionCommandHandler(
    ITenantRepository tenants,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    ISubscriptionInvoiceRepository subscriptionInvoices,
    IBillingLifecycleDecisionService decisionService,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ApplyOrganizationSubscriptionBillingDecisionCommandHandler> logger
) : IRequestHandler<ApplyOrganizationSubscriptionBillingDecisionCommand, ApplyOrganizationSubscriptionBillingDecisionResult>
{
    public async ValueTask<ApplyOrganizationSubscriptionBillingDecisionResult> Handle(
        ApplyOrganizationSubscriptionBillingDecisionCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        OrganizationSubscription subscription =
            await organizationSubscriptions.GetLatestForTenantAsync(tenant.Id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(OrganizationSubscription), command.OrganizationId);

        DateOnly businessDate = DateOnly.FromDateTime(DhakaTimeZone.ToLocal(clock.UtcNow).DateTime);

        IReadOnlyList<SubscriptionInvoice> invoices =
            await subscriptionInvoices.GetOutstandingAsync(tenant.Id.Value, cancellationToken);

        BillingLifecycleDecisionResult decision = decisionService.Evaluate(subscription, invoices, businessDate);

        OrganizationSubscriptionStatus previousStatus = subscription.Status;
        bool transitionApplied = true;

        switch (decision.RecommendedAction)
        {
            case BillingLifecycleRecommendedAction.MarkPastDue:
                subscription.MarkPastDue();
                break;
            case BillingLifecycleRecommendedAction.Reactivate:
                subscription.Reactivate();
                break;
            case BillingLifecycleRecommendedAction.Restrict:
                subscription.Restrict(clock.UtcNow);
                break;
            case BillingLifecycleRecommendedAction.Expire:
                subscription.Expire(clock.UtcNow);
                break;
            case BillingLifecycleRecommendedAction.NoAction:
            default:
                transitionApplied = false;
                break;
        }

        if (transitionApplied)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Organization {TenantId} subscription {SubscriptionId} billing lifecycle decision applied: " +
                "{PreviousStatus} -> {ResultingStatus} ({Recommendation})",
                tenant.Id, subscription.Id, previousStatus, subscription.Status, decision.RecommendedAction);
        }

        return new ApplyOrganizationSubscriptionBillingDecisionResult(
            tenant.Id.Value,
            subscription.Id.Value,
            previousStatus.ToString(),
            subscription.Status.ToString(),
            decision.RecommendedAction.ToString(),
            transitionApplied,
            decision.MaxDaysOverdue,
            decision.Reason);
    }
}
