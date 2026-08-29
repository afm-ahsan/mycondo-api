namespace MyCondo.Application.Features.Platform.DTOs;

/// <summary>The organization's most recent <c>OrganizationSubscription</c> row — lifecycle status,
/// exact package/version, billing terms, and the immutable commercial snapshot captured at
/// creation/change time (ADR-033 §11). Never re-derived from the live package version afterward, so
/// this always reflects what was actually agreed, not current list pricing.</summary>
public sealed record SubscriptionSnapshotDto(
    Guid SubscriptionId,
    string Status,
    Guid PackageId,
    string PackageCode,
    string PackageName,
    Guid PackageVersionId,
    int PackageVersion,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? NextBillingDate,
    bool AutoRenew,
    string Currency,
    decimal BasePrice,
    decimal Discount,
    decimal EffectivePrice,
    DateTimeOffset ActivatedAtUtc,
    DateTimeOffset? RestrictedAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    DateTimeOffset? CanceledAtUtc);
