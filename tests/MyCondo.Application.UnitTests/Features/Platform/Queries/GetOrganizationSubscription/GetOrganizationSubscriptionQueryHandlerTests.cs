using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationSubscription;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.GetOrganizationSubscription;

public class GetOrganizationSubscriptionQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IOrganizationSubscriptionRepository _organizationSubscriptions =
        Substitute.For<IOrganizationSubscriptionRepository>();
    private readonly ISubscriptionPackageVersionRepository _subscriptionPackageVersions =
        Substitute.For<ISubscriptionPackageVersionRepository>();
    private readonly ISubscriptionPackageRepository _subscriptionPackages = Substitute.For<ISubscriptionPackageRepository>();
    private readonly ITenantEntitlementService _tenantEntitlementService = Substitute.For<ITenantEntitlementService>();

    private GetOrganizationSubscriptionQueryHandler CreateHandler() => new(
        _tenants, _organizationSubscriptions, _subscriptionPackageVersions, _subscriptionPackages, _tenantEntitlementService);

    private static Tenant CreateActiveTenant()
    {
        Tenant tenant = Tenant.Provision("Alpha Residences", "alpha-residences", NowUtc);
        tenant.Activate(NowUtc);
        return tenant;
    }

    [Fact]
    public async Task Returns_Subscription_Snapshot_And_Features_When_A_Subscription_Exists()
    {
        Tenant tenant = CreateActiveTenant();
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);

        SubscriptionPackage package = SubscriptionPackage.Create("PRO", "Professional", description: null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, version: 1, effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime).AddDays(-30),
            effectiveUntil: null, monthlyPrice: 1500m, quarterlyPrice: null, semiAnnualPrice: null,
            annualPrice: null, currency: "BDT");
        version.Activate();
        package.Activate();
        package.SetCurrentVersion(version.Id);

        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenant.Id.Value, version.Id, BillingCycle.Monthly,
            startDate: DateOnly.FromDateTime(NowUtc.UtcDateTime).AddDays(-30), endDate: null, nextBillingDate: null,
            basePrice: 1500m, discount: 100m, currency: "BDT", activatedAtUtc: NowUtc, autoRenew: true);

        _organizationSubscriptions.GetLatestForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>())
            .Returns(subscription);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([version]);
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([package]);

        EffectiveEntitlement feature = new("finance.expenses", true, FeatureEntitlementType.Boolean, null, EntitlementSource.Package);
        _tenantEntitlementService.GetEffectiveEntitlementDetails(tenant.Id.Value, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EffectiveEntitlement>)[feature]);

        OrganizationSubscriptionDto result = await CreateHandler().Handle(
            new GetOrganizationSubscriptionQuery(tenant.Id.Value), CancellationToken.None);

        result.HasSubscription.Should().BeTrue();
        result.Subscription.Should().NotBeNull();
        result.Subscription!.PackageCode.Should().Be("PRO");
        result.Subscription.PackageVersion.Should().Be(1);
        result.Subscription.Status.Should().Be(nameof(OrganizationSubscriptionStatus.Active));
        result.Subscription.BillingCycle.Should().Be(nameof(BillingCycle.Monthly));
        result.Subscription.BasePrice.Should().Be(1500m);
        result.Subscription.Discount.Should().Be(100m);
        result.Subscription.EffectivePrice.Should().Be(1400m);
        result.Subscription.AutoRenew.Should().BeTrue();

        result.Features.Should().ContainSingle();
        result.Features[0].FeatureKey.Should().Be("finance.expenses");
        result.Features[0].Source.Should().Be(nameof(EntitlementSource.Package));
    }

    [Fact]
    public async Task Returns_Null_Subscription_When_None_Has_Ever_Been_Provisioned()
    {
        Tenant tenant = CreateActiveTenant();
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _organizationSubscriptions.GetLatestForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>())
            .Returns((OrganizationSubscription?)null);
        _tenantEntitlementService.GetEffectiveEntitlementDetails(tenant.Id.Value, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<EffectiveEntitlement>)[]);

        OrganizationSubscriptionDto result = await CreateHandler().Handle(
            new GetOrganizationSubscriptionQuery(tenant.Id.Value), CancellationToken.None);

        result.HasSubscription.Should().BeFalse();
        result.Subscription.Should().BeNull();
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () =>
            await CreateHandler().Handle(new GetOrganizationSubscriptionQuery(organizationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
