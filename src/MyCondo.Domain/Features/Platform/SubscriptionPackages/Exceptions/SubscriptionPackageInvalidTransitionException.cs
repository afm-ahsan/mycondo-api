using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

/// <summary>Guards <see cref="SubscriptionPackage"/>'s Draft → Active → Retired lifecycle (ADR-033 §7) —
/// thrown for any transition attempt other than Draft→Active or {Draft,Active}→Retired.</summary>
public sealed class SubscriptionPackageInvalidTransitionException(
    SubscriptionPackageId id, SubscriptionPackageStatus from, SubscriptionPackageStatus to)
    : DomainException($"Subscription package {id} cannot transition from {from} to {to}.");
