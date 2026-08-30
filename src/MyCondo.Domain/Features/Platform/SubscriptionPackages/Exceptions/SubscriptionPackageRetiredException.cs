using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

/// <summary>A retired <see cref="SubscriptionPackage"/> is terminal (ADR-033 §7) — its current-version
/// pointer can no longer change; retiring is the only way to stop a package being offered.</summary>
public sealed class SubscriptionPackageRetiredException(SubscriptionPackageId id)
    : DomainException($"Subscription package {id} is retired and can no longer be modified.");
