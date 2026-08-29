using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.ChangeOrganizationSubscription;

/// <summary>
/// Changes an existing organization's assigned subscription package/version, billing cycle, and
/// AutoRenew (ADR-033 Task 13B) — deliberately as narrow as
/// <see cref="Commands.ProvisionOrganizationWithAdmin.ProvisionOrganizationWithAdminCommandHandler"/>'s
/// own subscription creation: no proration, negotiated pricing, invoices, or scheduler behavior. Only
/// an Active/PastDue subscription (<see cref="IOrganizationSubscriptionRepository.GetCurrentForTenantAsync"/>)
/// can be changed — <see cref="OrganizationSubscription.ChangePackageVersion"/> itself guards that.
/// </summary>
public sealed class ChangeOrganizationSubscriptionCommandHandler(
    ITenantRepository tenants,
    IOrganizationSubscriptionRepository organizationSubscriptions,
    ISubscriptionPackageRepository subscriptionPackages,
    ISubscriptionPackageVersionRepository subscriptionPackageVersions,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ChangeOrganizationSubscriptionCommandHandler> logger
) : IRequestHandler<ChangeOrganizationSubscriptionCommand>
{
    public async ValueTask<Unit> Handle(ChangeOrganizationSubscriptionCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        OrganizationSubscription subscription =
            await organizationSubscriptions.GetCurrentForTenantAsync(tenant.Id.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(OrganizationSubscription), command.OrganizationId);

        SubscriptionPackageVersion packageVersion = await ResolveAssignablePackageVersionAsync(
            command.SubscriptionPackageVersionId, cancellationToken);

        // The caller-selected billing cycle must be one the newly assigned package version actually
        // prices — mirrors ProvisionOrganizationWithAdminCommandHandler's identical rule for creation;
        // a version with no price for the requested cycle fails with UnsupportedBillingCycleException,
        // which is the correct rejection here too.
        decimal basePrice = OrganizationSubscriptionCommercialTerms.ResolveBasePrice(packageVersion, command.BillingCycle);

        subscription.ChangePackageVersion(
            packageVersion.Id, command.BillingCycle, basePrice, packageVersion.Currency, command.AutoRenew);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Organization {TenantId} subscription {SubscriptionId} changed to package version {PackageVersionId}",
            tenant.Id, subscription.Id, packageVersion.Id);

        return Unit.Value;
    }

    /// <summary>
    /// Identical validation to <c>ProvisionOrganizationWithAdminCommandHandler.ResolveAssignablePackageVersionAsync</c>
    /// — duplicated rather than shared, following that handler's own established precedent (see its doc
    /// comment) of not introducing a new repository query method or cross-command dependency for this
    /// small catalogue lookup.
    /// </summary>
    private async Task<SubscriptionPackageVersion> ResolveAssignablePackageVersionAsync(
        Guid subscriptionPackageVersionId, CancellationToken cancellationToken)
    {
        SubscriptionPackageVersionId versionId = new(subscriptionPackageVersionId);

        SubscriptionPackageVersion? version = (await subscriptionPackageVersions.GetAllAsync(cancellationToken))
            .SingleOrDefault(v => v.Id == versionId);
        if (version is null)
        {
            throw new NotFoundException(nameof(SubscriptionPackageVersion), subscriptionPackageVersionId);
        }

        if (version.Status != SubscriptionPackageVersionStatus.Active)
        {
            throw new ConflictException(
                $"Subscription package version '{subscriptionPackageVersionId}' is not commercially active and cannot be assigned.");
        }

        SubscriptionPackage? package = (await subscriptionPackages.GetAllAsync(cancellationToken))
            .SingleOrDefault(p => p.Id == version.PackageId);
        if (package is null || package.Status != SubscriptionPackageStatus.Active || package.CurrentVersionId != version.Id)
        {
            throw new ConflictException(
                $"Subscription package version '{subscriptionPackageVersionId}' does not belong to an active, currently offered subscription package.");
        }

        // The "LEGACY-MIGRATION-GRANDFATHERED" package (LegacyMigrationSubscriptionBackfillSeeder) is a
        // migration-bridge artifact, never a commercial offering — an organization must never be moved
        // onto it, even explicitly. Literal duplicated here, not referenced from Infrastructure, to keep
        // Application's dependency direction intact (same rationale as the provisioning handler).
        if (string.Equals(package.Code, "LEGACY-MIGRATION-GRANDFATHERED", StringComparison.Ordinal))
        {
            throw new ConflictException(
                "The legacy migration grandfathered package cannot be assigned to an organization.");
        }

        DateOnly today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        if (version.EffectiveFrom > today || (version.EffectiveUntil is not null && version.EffectiveUntil < today))
        {
            throw new ConflictException(
                $"Subscription package version '{subscriptionPackageVersionId}' is not effective as of {today:yyyy-MM-dd}.");
        }

        return version;
    }
}
