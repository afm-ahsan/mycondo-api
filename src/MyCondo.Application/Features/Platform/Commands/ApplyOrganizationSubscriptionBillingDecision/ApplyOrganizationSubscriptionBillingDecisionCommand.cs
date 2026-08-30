using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.ApplyOrganizationSubscriptionBillingDecision;

/// <summary>
/// "Evaluate current billing state and apply the currently valid recommendation" (ADR-034 Task 14F) —
/// the client selects nothing beyond which organization to evaluate. The Task 14E decision service is
/// re-run against freshly loaded persisted state inside the handler; the caller cannot supply a
/// recommendation, a lifecycle status, or days-overdue.
/// </summary>
public sealed record ApplyOrganizationSubscriptionBillingDecisionCommand(Guid OrganizationId)
    : IRequest<ApplyOrganizationSubscriptionBillingDecisionResult>;

/// <summary>
/// <see cref="PreviousStatus"/>, <see cref="ResultingStatus"/>, and <see cref="Recommendation"/> are
/// serialized as their enum <c>string</c> name (via <c>.ToString()</c> in the handler), matching every
/// other status-shaped field in the Platform/subscription billing API contract (e.g.
/// <c>OrganizationSubscriptionDto.TenantStatus</c>, <c>PlatformSubscriptionInvoiceDetailDto.Status</c>,
/// <c>TenantBillingResolutionDto.SubscriptionStatus</c>) — no field here should ever go back to a raw
/// enum type, which would serialize as an unstable ordinal integer (ADR-034 Task 14M closure).
/// </summary>
public sealed record ApplyOrganizationSubscriptionBillingDecisionResult(
    Guid OrganizationId,
    Guid SubscriptionId,
    string PreviousStatus,
    string ResultingStatus,
    string Recommendation,
    bool TransitionApplied,
    int MaxDaysOverdue,
    string Reason);
