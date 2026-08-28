using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Common.Behaviors;

/// <summary>
/// Enforces the two independent lifecycle axes ADR-032 §4/§6 requires — Organization
/// (<see cref="ITenantLifecycleAccessService"/>) and Subscription (<see cref="ISubscriptionLifecycleAccessService"/>)
/// — as one central choke point, deliberately separate from <see cref="FeatureEntitlementBehavior{TMessage,TResponse}"/>
/// (never merged into it, ADR-032 Task 10 §4). Registered <b>before</b> <c>FeatureEntitlementBehavior</c> in
/// <c>Application/DependencyInjection.cs</c> — a cheap, coarse lifecycle check should fail fast ahead of a
/// finer-grained per-feature lookup (ADR-033 §16's own reserved-extension-point note), and a hard
/// organization lockout must never leak as a misleading <c>feature_not_entitled</c> (ADR-032 Task 10 §31/§32).
///
/// <para><b>Applies to every tenant-scheme request structurally</b> — no per-request marker needed for the
/// Organization axis or for the Subscription axis's coarse Full/ReadOnly decision: both are derived purely
/// from <see cref="ICurrentUserProvider.TenantId"/> being present, exactly the same structural signal
/// <see cref="FeatureEntitlementBehavior{TMessage,TResponse}"/> already relies on to distinguish tenant-scheme
/// from Platform-scheme requests (Platform JWTs structurally carry no <c>TenantId</c> claim, ADR-019). A
/// request with no tenant context (Platform-scheme, or an anonymous pre-auth request such as
/// Login/Register/Refresh) is not this behavior's concern and passes through unconditionally.</para>
///
/// <para><b>Read/write classification only matters once <see cref="TenantAccessMode.ReadOnly"/> applies</b>
/// (ADR-032 Task 10 §13/§59): <see cref="ILifecycleReadOperation"/> and <see cref="IBillingResolutionOperation"/>
/// pass through; everything else — including every request that carries no marker at all — is treated as a
/// blocked write (fail closed). This is a deliberate pilot rollout on a small representative set, not a full
/// sweep; see <see cref="ILifecycleReadOperation"/>'s doc comment.</para>
/// </summary>
public sealed class TenantLifecycleBehavior<TMessage, TResponse>(
    ICurrentUserProvider currentUser,
    ITenantLifecycleAccessService organizationLifecycle,
    ISubscriptionLifecycleAccessService subscriptionLifecycle
) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        Guid? tenantId = currentUser.TenantId;
        if (tenantId is null)
        {
            return await next(message, cancellationToken);
        }

        TenantStatus organizationStatus = await organizationLifecycle.GetTenantStatusAsync(tenantId.Value, cancellationToken);
        if (!OrganizationLifecyclePolicy.AllowsAccess(organizationStatus))
        {
            throw new OrganizationLifecycleAccessDeniedException(tenantId.Value, organizationStatus);
        }

        SubscriptionLifecycleAccess subscriptionAccess = await subscriptionLifecycle.GetAccessAsync(tenantId.Value, cancellationToken);
        if (subscriptionAccess.Mode == TenantAccessMode.Full)
        {
            return await next(message, cancellationToken);
        }

        if (message is ILifecycleReadOperation or IBillingResolutionOperation)
        {
            return await next(message, cancellationToken);
        }

        throw new SubscriptionLifecycleAccessDeniedException(tenantId.Value, subscriptionAccess.SourceStatus!.Value);
    }
}
