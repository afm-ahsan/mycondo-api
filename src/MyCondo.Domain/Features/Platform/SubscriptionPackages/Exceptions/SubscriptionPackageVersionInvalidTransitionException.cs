using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

/// <summary>Guards <see cref="SubscriptionPackageVersion"/>'s Draft → Active → Superseded lifecycle
/// (ADR-033 §8) — thrown for any transition attempt other than Draft→Active or Active→Superseded.</summary>
public sealed class SubscriptionPackageVersionInvalidTransitionException(
    SubscriptionPackageVersionId id, SubscriptionPackageVersionStatus from, SubscriptionPackageVersionStatus to)
    : DomainException($"Subscription package version {id} cannot transition from {from} to {to}.");
