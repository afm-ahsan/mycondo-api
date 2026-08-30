using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationSubscription;

/// <summary>Read-only platform view of an organization's subscription and effective feature state
/// (ADR-033 Task 13A). Reuses <see cref="ITenantEntitlementService"/> for feature resolution — this
/// handler implements no entitlement precedence of its own.</summary>
public sealed class GetOrganizationSubscriptionQueryHandler(
    ITenantRepository tenants,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    ISubscriptionPackageVersionRepository subscriptionPackageVersions,
    ISubscriptionPackageRepository subscriptionPackages,
    ITenantEntitlementService tenantEntitlementService
) : IRequestHandler<GetOrganizationSubscriptionQuery, OrganizationSubscriptionDto>
{
    public async ValueTask<OrganizationSubscriptionDto> Handle(
        GetOrganizationSubscriptionQuery query, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(query.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), query.OrganizationId);

        // The latest subscription regardless of status (Active/PastDue/Restricted/Expired/Canceled) —
        // not GetCurrentForTenantAsync — so a platform admin can see a lapsed subscription's terms
        // rather than nothing. null here means no subscription has ever been provisioned; that
        // distinction, and the separate legacy "no subscription -> Full lifecycle access" compatibility
        // rule it interacts with elsewhere, are out of scope for this read model to alter.
        OrganizationSubscription? subscription =
            await organizationSubscriptions.GetLatestForTenantAsync(tenant.Id.Value, cancellationToken);

        SubscriptionSnapshotDto? snapshot = subscription is null
            ? null
            : await BuildSnapshotAsync(subscription, cancellationToken);

        IReadOnlyList<EffectiveEntitlement> entitlements =
            await tenantEntitlementService.GetEffectiveEntitlementDetails(tenant.Id.Value, cancellationToken);

        List<EffectiveFeatureEntitlementDto> features = entitlements
            .Select(e => new EffectiveFeatureEntitlementDto(
                e.FeatureKey, e.Enabled, e.EntitlementType.ToString(), e.LimitValue, e.Source.ToString()))
            .ToList();

        return new OrganizationSubscriptionDto(
            TenantId: tenant.Id.Value,
            TenantName: tenant.Name,
            TenantCode: tenant.Code,
            TenantStatus: tenant.Status.ToString(),
            HasSubscription: subscription is not null,
            Subscription: snapshot,
            Features: features);
    }

    private async Task<SubscriptionSnapshotDto> BuildSnapshotAsync(
        OrganizationSubscription subscription, CancellationToken cancellationToken)
    {
        // Neither repository exposes a by-id lookup yet — the same in-memory-filter pattern
        // ProvisionOrganizationWithAdminCommandHandler already established for this small catalogue.
        SubscriptionPackageVersion version = (await subscriptionPackageVersions.GetAllAsync(cancellationToken))
                .SingleOrDefault(v => v.Id == subscription.PackageVersionId)
            ?? throw new NotFoundException(nameof(SubscriptionPackageVersion), subscription.PackageVersionId.Value);

        SubscriptionPackage package = (await subscriptionPackages.GetAllAsync(cancellationToken))
                .SingleOrDefault(p => p.Id == version.PackageId)
            ?? throw new NotFoundException(nameof(SubscriptionPackage), version.PackageId.Value);

        return new SubscriptionSnapshotDto(
            SubscriptionId: subscription.Id.Value,
            Status: subscription.Status.ToString(),
            PackageId: package.Id.Value,
            PackageCode: package.Code,
            PackageName: package.Name,
            PackageVersionId: version.Id.Value,
            PackageVersion: version.Version,
            BillingCycle: subscription.BillingCycle.ToString(),
            StartDate: subscription.StartDate,
            EndDate: subscription.EndDate,
            NextBillingDate: subscription.NextBillingDate,
            AutoRenew: subscription.AutoRenew,
            Currency: subscription.Currency,
            BasePrice: subscription.BasePrice,
            Discount: subscription.Discount,
            EffectivePrice: subscription.EffectivePrice,
            ActivatedAtUtc: subscription.ActivatedAt,
            RestrictedAtUtc: subscription.RestrictedAt,
            ExpiredAtUtc: subscription.ExpiredAt,
            CanceledAtUtc: subscription.CanceledAt);
    }
}
