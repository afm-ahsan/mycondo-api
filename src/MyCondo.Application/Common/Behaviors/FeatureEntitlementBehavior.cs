using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;

namespace MyCondo.Application.Common.Behaviors;

/// <summary>
/// Enforces the Feature Catalogue entitlement gate (ADR-033 §16) for requests declaring
/// <see cref="IRequiresFeature"/>. Registered after <see cref="ValidationBehavior{TMessage,TResponse}"/> —
/// the last gate before the handler runs, so a structurally invalid request never spends an entitlement
/// lookup. Delegates the actual precedence decision to <see cref="ITenantEntitlementService"/>; this
/// behavior never queries package/override data itself (ADR-033 §16/Task 06 §7).
/// </summary>
public sealed class FeatureEntitlementBehavior<TMessage, TResponse>(
    ICurrentUserProvider currentUser,
    ITenantEntitlementService entitlements
) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (message is not IRequiresFeature requiresFeature)
        {
            return await next(message, cancellationToken);
        }

        // A missing tenant context on an IRequiresFeature request is a code defect (there is no tenant
        // to resolve entitlement against, e.g. a Platform-scheme request mistakenly marked) — a hard
        // failure, not a silent pass-through (ADR-033 §16).
        Guid tenantId = currentUser.TenantId
            ?? throw new InvalidOperationException(
                $"{typeof(TMessage).Name} implements {nameof(IRequiresFeature)} but no tenant context is present.");

        bool isEnabled = await entitlements.IsFeatureEnabled(tenantId, requiresFeature.FeatureKey, cancellationToken);
        if (!isEnabled)
        {
            throw new FeatureNotEntitledException(requiresFeature.FeatureKey);
        }

        return await next(message, cancellationToken);
    }
}
