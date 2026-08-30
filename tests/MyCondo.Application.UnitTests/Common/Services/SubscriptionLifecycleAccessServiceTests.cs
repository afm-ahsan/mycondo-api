using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

/// <summary>Application-layer tests for <see cref="SubscriptionLifecycleAccessService"/> (ADR-032 Task 10
/// §46/§47/§48) — verifies the two-query strategy: <c>GetCurrentForTenantAsync</c> answers the common case
/// (Active/PastDue/Restricted) alone; <c>GetLatestForTenantAsync</c> is only consulted, and only once, when
/// no current subscription exists, to distinguish Expired/Canceled from "never provisioned."</summary>
public class SubscriptionLifecycleAccessServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IOrganizationSubscriptionRepository _subscriptions = Substitute.For<IOrganizationSubscriptionRepository>();

    private SubscriptionLifecycleAccessService CreateSut() => new(_subscriptions);

    private static OrganizationSubscription NewSubscription() => OrganizationSubscription.Create(
        TenantId, SubscriptionPackageVersionId.New(),
        BillingCycle.Monthly, new DateOnly(2026, 1, 1), null, null, 1000m, 0m, "BDT",
        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), autoRenew: false);

    [Theory]
    [InlineData(OrganizationSubscriptionStatus.Active, TenantAccessMode.Full)]
    [InlineData(OrganizationSubscriptionStatus.PastDue, TenantAccessMode.Full)]
    [InlineData(OrganizationSubscriptionStatus.Restricted, TenantAccessMode.ReadOnly)]
    public async Task Uses_The_Current_Subscription_Without_A_Second_Query_When_One_Exists(
        OrganizationSubscriptionStatus status, TenantAccessMode expectedMode)
    {
        OrganizationSubscription subscription = NewSubscription();
        if (status == OrganizationSubscriptionStatus.PastDue)
        {
            subscription.MarkPastDue();
        }
        if (status == OrganizationSubscriptionStatus.Restricted)
        {
            subscription.MarkPastDue();
            subscription.Restrict(DateTimeOffset.UtcNow);
        }
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns(subscription);

        SubscriptionLifecycleAccess access = await CreateSut().GetAccessAsync(TenantId, CancellationToken.None);

        access.Mode.Should().Be(expectedMode);
        await _subscriptions.DidNotReceive().GetLatestForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falls_Back_To_The_Latest_Subscription_When_No_Current_One_Exists()
    {
        OrganizationSubscription subscription = NewSubscription();
        subscription.MarkPastDue();
        subscription.Restrict(DateTimeOffset.UtcNow);
        subscription.Expire(DateTimeOffset.UtcNow);
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns((OrganizationSubscription?)null);
        _subscriptions.GetLatestForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns(subscription);

        SubscriptionLifecycleAccess access = await CreateSut().GetAccessAsync(TenantId, CancellationToken.None);

        access.Mode.Should().Be(TenantAccessMode.ReadOnly);
        access.SourceStatus.Should().Be(OrganizationSubscriptionStatus.Expired);
    }

    [Fact]
    public async Task Resolves_To_Full_When_No_Subscription_Has_Ever_Been_Provisioned()
    {
        // ADR-032 Task 10 §21/§77 — no subscription-aware provisioning path exists yet, so this is the
        // normal state for every tenant provisioned since Task 04's legacy-backfill cutoff.
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns((OrganizationSubscription?)null);
        _subscriptions.GetLatestForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns((OrganizationSubscription?)null);

        SubscriptionLifecycleAccess access = await CreateSut().GetAccessAsync(TenantId, CancellationToken.None);

        access.Mode.Should().Be(TenantAccessMode.Full);
        access.SourceStatus.Should().BeNull();
    }
}
