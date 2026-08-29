using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.CancelOrganizationSubscription;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.CancelOrganizationSubscription;

public class CancelOrganizationSubscriptionCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IOrganizationSubscriptionRepository _subscriptions = Substitute.For<IOrganizationSubscriptionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CancelOrganizationSubscriptionCommandHandlerTests() => _clock.UtcNow.Returns(NowUtc);

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

    private CancelOrganizationSubscriptionCommandHandler CreateHandler() =>
        new(_tenants, _subscriptions, _unitOfWork, _clock, Substitute.For<ILogger<CancelOrganizationSubscriptionCommandHandler>>());

    [Fact]
    public async Task Cancels_An_Active_Subscription()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _subscriptions.GetLatestForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(subscription);

        await CreateHandler().Handle(new CancelOrganizationSubscriptionCommand(tenant.Id.Value), CancellationToken.None);

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Canceled);
        subscription.CanceledAt.Should().Be(NowUtc);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancels_A_Past_Due_Subscription()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value);
        subscription.MarkPastDue();
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _subscriptions.GetLatestForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(subscription);

        await CreateHandler().Handle(new CancelOrganizationSubscriptionCommand(tenant.Id.Value), CancellationToken.None);

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Canceled);
    }

    [Fact]
    public async Task Throws_When_Subscription_Is_Restricted()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value);
        subscription.MarkPastDue();
        subscription.Restrict(NowUtc);
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _subscriptions.GetLatestForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(subscription);

        Func<Task> act = async () => await CreateHandler().Handle(
            new CancelOrganizationSubscriptionCommand(tenant.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<OrganizationSubscriptionInvalidTransitionException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new CancelOrganizationSubscriptionCommand(organizationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
