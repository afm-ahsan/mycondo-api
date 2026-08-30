using Mediator;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Application.Features.Platform.Queries.GetSubscriptionPackageOptions;

/// <summary>
/// Read-only platform view of commercially assignable subscription packages (ADR-033 Task 13E.1) — the
/// dependency-fix that lets the organization provisioning wizard offer real choices instead of a
/// hard-coded/missing selection. Mirrors, rather than reuses, the eligibility checks
/// <c>ProvisionOrganizationWithAdminCommandHandler.ResolveAssignablePackageVersionAsync</c> already
/// enforces (Task 12A/12B): package Active, current version Active, version within its effective
/// window, and the migration-bridge "LEGACY-MIGRATION-GRANDFATHERED" package excluded. This read model
/// must never offer a package/version choice provisioning itself would reject. Duplicating these checks
/// as a filter predicate (rather than extracting a shared service) follows ADR-033 Task 13E.1 §2's
/// explicit instruction not to perform a broad refactor solely to eliminate this duplication.
///
/// Only a package's <see cref="SubscriptionPackage.CurrentVersionId"/> is ever returned — at most one
/// version per package can be Active at a time (see <see cref="SubscriptionPackageVersion"/>'s "current
/// version" invariant), so there is exactly one eligible version per eligible package.
/// </summary>
public sealed class GetSubscriptionPackageOptionsQueryHandler(
    ISubscriptionPackageRepository subscriptionPackages,
    ISubscriptionPackageVersionRepository subscriptionPackageVersions,
    IClock clock
) : IRequestHandler<GetSubscriptionPackageOptionsQuery, List<SubscriptionPackageOptionDto>>
{
    public async ValueTask<List<SubscriptionPackageOptionDto>> Handle(
        GetSubscriptionPackageOptionsQuery query, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        List<SubscriptionPackage> packages = await subscriptionPackages.GetAllAsync(cancellationToken);
        List<SubscriptionPackageVersion> versions = await subscriptionPackageVersions.GetAllAsync(cancellationToken);

        List<SubscriptionPackageOptionDto> options = [];

        foreach (SubscriptionPackage package in packages)
        {
            if (package.Status != SubscriptionPackageStatus.Active)
            {
                continue;
            }

            // Migration-bridge artifact (LegacyMigrationSubscriptionBackfillSeeder), never a commercial
            // offering — literal duplicated here rather than referenced from Infrastructure, matching
            // ProvisionOrganizationWithAdminCommandHandler's identical duplication and its reasoning
            // (Application must not reference Infrastructure).
            if (string.Equals(package.Code, "LEGACY-MIGRATION-GRANDFATHERED", StringComparison.Ordinal))
            {
                continue;
            }

            if (package.CurrentVersionId is null)
            {
                continue;
            }

            SubscriptionPackageVersion? version =
                versions.SingleOrDefault(v => v.Id == package.CurrentVersionId);
            if (version is null || version.Status != SubscriptionPackageVersionStatus.Active)
            {
                continue;
            }

            if (version.EffectiveFrom > today || (version.EffectiveUntil is not null && version.EffectiveUntil < today))
            {
                continue;
            }

            List<SubscriptionPackageBillingCycleOptionDto> billingCycles = [];
            AddCycleIfPriced(billingCycles, BillingCycle.Monthly, version.MonthlyPrice);
            AddCycleIfPriced(billingCycles, BillingCycle.Quarterly, version.QuarterlyPrice);
            AddCycleIfPriced(billingCycles, BillingCycle.SemiAnnual, version.SemiAnnualPrice);
            AddCycleIfPriced(billingCycles, BillingCycle.Annual, version.AnnualPrice);

            if (billingCycles.Count == 0)
            {
                // No billing cycle is priced on this version — nothing a provisioning caller could
                // actually submit against ResolveBasePrice, so it is not a real choice.
                continue;
            }

            options.Add(new SubscriptionPackageOptionDto(
                package.Id.Value,
                package.Code,
                package.Name,
                version.Id.Value,
                version.Version,
                version.Currency,
                billingCycles));
        }

        return options.OrderBy(o => o.PackageName, StringComparer.Ordinal).ToList();
    }

    private static void AddCycleIfPriced(
        List<SubscriptionPackageBillingCycleOptionDto> billingCycles, BillingCycle cycle, decimal? price)
    {
        if (price is not null)
        {
            billingCycles.Add(new SubscriptionPackageBillingCycleOptionDto(cycle.ToString(), price.Value));
        }
    }
}
