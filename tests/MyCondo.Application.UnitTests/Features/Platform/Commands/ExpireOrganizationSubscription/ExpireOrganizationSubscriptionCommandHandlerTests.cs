using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.ExpireOrganizationSubscription;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.ExpireOrganizationSubscription;

public class ExpireOrganizationSubscriptionCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IOrganizationSubscriptionRepository _subscriptions = Substitute.For<IOrganizationSubscriptionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ExpireOrganizationSubscriptionCommandHandlerTests() => _clock.UtcNow.Returns(NowUtc);

    private static Tenant CreateActiveTenant()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        tenant.Activate(NowUtc);
        return tenant;
    }

    private static OrganizationSubscription CreateSubscription(Guid tenantId) =>
        OrganizationSubscription.Create(
            tenantId, SubscriptionPackageVersionId.New(), BillingCycle.Monthly,
            DateOnly.FromDateTime(NowUtc.UtcDateTime), null, null, 8000m, 0m, "BDT", NowUtc, autoRenew: false);

    private ExpireOrganizationSubscriptionCommandHandler CreateHandler() =>
        new(_tenants, _subscriptions, _unitOfWork, _clock, Substitute.For<ILogger<ExpireOrganizationSubscriptionCommandHandler>>());

    [Fact]
    public async Task Expires_A_Restricted_Subscription()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value);
        subscription.MarkPastDue();
        subscription.Restrict(NowUtc);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _subscriptions.GetLatestForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(subscription);

        await CreateHandler().Handle(new ExpireOrganizationSubscriptionCommand(tenant.Id.Value), CancellationToken.None);

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Expired);
        subscription.ExpiredAt.Should().Be(NowUtc);
    }

    [Fact]
    public async Task Expires_A_Canceled_Subscription_Even_Though_It_Is_Not_The_Current_One()
    {
        // GetCurrentForTenantAsync deliberately excludes Canceled subscriptions (only Active/PastDue/
        // Restricted count as "current"); this handler must use GetLatestForTenantAsync instead so a
        // Canceled subscription — one of Expire's two valid source statuses — is still reachable.
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value);
        subscription.Cancel(NowUtc);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _subscriptions.GetLatestForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(subscription);

        await CreateHandler().Handle(new ExpireOrganizationSubscriptionCommand(tenant.Id.Value), CancellationToken.None);

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Expired);
    }

    [Fact]
    public async Task Throws_When_Subscription_Is_Active()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _subscriptions.GetLatestForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(subscription);

        Func<Task> act = async () => await CreateHandler().Handle(
            new ExpireOrganizationSubscriptionCommand(tenant.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<OrganizationSubscriptionInvalidTransitionException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new ExpireOrganizationSubscriptionCommand(organizationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
