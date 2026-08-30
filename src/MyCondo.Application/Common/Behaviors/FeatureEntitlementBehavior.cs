using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;

namespace MyCondo.Application.Common.Behaviors;

/// <summary>
/// Enforces the Feature Catalogue entitlement gate (ADR-033 §16) for requests declaring
/// <see cref="IRequiresFeature"/> (static key, read straight off the request) or
/// <see cref="IRequiresResolvedFeature"/> (ADR-033 Task 09A — key resolved from a persisted resource via
/// the matching <see cref="IRequestFeatureResolver{TRequest}"/>). Registered after
/// <see cref="ValidationBehavior{TMessage,TResponse}"/> — the last gate before the handler runs, so a
/// structurally invalid request never spends an entitlement lookup. Delegates the actual precedence
/// decision to <see cref="ITenantEntitlementService"/>; this behavior never queries package/override data
/// itself (ADR-033 §16/Task 06 §7), and never derives a feature from anything but
/// <see cref="IRequiresFeature.FeatureKey"/> or a resolver's persisted-resource lookup — never from
/// permissions, roles, or request-supplied type/category strings (Task 09A §19).
/// </summary>
public sealed class FeatureEntitlementBehavior<TMessage, TResponse>(
    ICurrentUserProvider currentUser,
    ITenantEntitlementService entitlements,
    IServiceProvider serviceProvider
) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (message is not IRequiresFeature and not IRequiresResolvedFeature)
        {
            return await next(message, cancellationToken);
        }

        // A missing tenant context on a feature-gated request is a code defect (there is no tenant to
        // resolve entitlement against, e.g. a Platform-scheme request mistakenly marked) — a hard
        // failure, not a silent pass-through (ADR-033 §16).
        Guid tenantId = currentUser.TenantId
            ?? throw new InvalidOperationException(
                $"{typeof(TMessage).Name} requires feature entitlement but no tenant context is present.");

        string featureKey = message is IRequiresFeature requiresFeature
            ? requiresFeature.FeatureKey
            : await ResolveFeatureKeyAsync(message, tenantId, cancellationToken);

        bool isEnabled = await entitlements.IsFeatureEnabled(tenantId, featureKey, cancellationToken);
        if (!isEnabled)
        {
            throw new FeatureNotEntitledException(featureKey);
        }

        return await next(message, cancellationToken);
    }

    private Task<string> ResolveFeatureKeyAsync(TMessage message, Guid tenantId, CancellationToken cancellationToken)
    {
        IRequestFeatureResolver<TMessage>? resolver = serviceProvider.GetService<IRequestFeatureResolver<TMessage>>();
        if (resolver is null)
        {
            throw new InvalidOperationException(
                $"{typeof(TMessage).Name} implements {nameof(IRequiresResolvedFeature)} but no " +
                $"{nameof(IRequestFeatureResolver<TMessage>)} is registered for it.");
        }

        return resolver.ResolveFeatureKeyAsync(message, tenantId, cancellationToken);
    }
}
