using Mediator;
using MyCondo.Application.Features.Platform.Services.BillingLifecycleDecision;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

namespace MyCondo.Application.Features.Platform.Commands.ApplyOrganizationSubscriptionBillingDecision;

/// <summary>
/// "Evaluate current billing state and apply the currently valid recommendation" (ADR-034 Task 14F) —
/// the client selects nothing beyond which organization to evaluate. The Task 14E decision service is
/// re-run against freshly loaded persisted state inside the handler; the caller cannot supply a
/// recommendation, a lifecycle status, or days-overdue.
/// </summary>
public sealed record ApplyOrganizationSubscriptionBillingDecisionCommand(Guid OrganizationId)
    : IRequest<ApplyOrganizationSubscriptionBillingDecisionResult>;

public sealed record ApplyOrganizationSubscriptionBillingDecisionResult(
    Guid OrganizationId,
    Guid SubscriptionId,
    OrganizationSubscriptionStatus PreviousStatus,
    OrganizationSubscriptionStatus ResultingStatus,
    BillingLifecycleRecommendedAction Recommendation,
    bool TransitionApplied,
    int MaxDaysOverdue,
    string Reason);
