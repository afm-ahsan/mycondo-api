using MyCondo.Domain.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;

/// <summary>Thrown when a subscription is created/changed with a <see cref="BillingCycle"/> whose
/// corresponding price column on the assigned <see cref="SubscriptionPackageVersion"/> is null — that
/// cycle is not offered for this package version (ADR-033 §9: "if MonthlyPrice = null, Monthly must
/// not be valid for a subscription to that version").</summary>
public sealed class UnsupportedBillingCycleException(SubscriptionPackageVersionId packageVersionId, BillingCycle billingCycle)
    : DomainException($"Subscription package version {packageVersionId} does not offer the {billingCycle} billing cycle.");
