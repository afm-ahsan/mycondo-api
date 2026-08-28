using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.UnitTests.Common.Abstractions;

/// <summary>
/// Exhaustive matrix tests for the two pure lifecycle policies (ADR-032 Task 10 §50/§81) — no database, no
/// Mediator pipeline, just the access decision itself. <see cref="TenantLifecycleBehaviorTests"/> covers
/// how these compose inside the pipeline; this class only covers the policy functions in isolation.
/// </summary>
public class LifecyclePolicyTests
{
    [Theory]
    [InlineData(TenantStatus.Active, true)]
    [InlineData(TenantStatus.PendingActivation, false)]
    [InlineData(TenantStatus.Suspended, false)]
    [InlineData(TenantStatus.Closed, false)]
    public void OrganizationLifecyclePolicy_Only_Active_Allows_Access(TenantStatus status, bool expectedAllowed)
    {
        OrganizationLifecyclePolicy.AllowsAccess(status).Should().Be(expectedAllowed);
    }

    [Fact]
    public void SubscriptionLifecyclePolicy_No_Subscription_Resolves_To_Full()
    {
        SubscriptionLifecycleAccess access = SubscriptionLifecyclePolicy.Evaluate(null);

        access.Mode.Should().Be(TenantAccessMode.Full);
        access.SourceStatus.Should().BeNull();
    }

    [Theory]
    [InlineData(OrganizationSubscriptionStatus.Active)]
    [InlineData(OrganizationSubscriptionStatus.PastDue)]
    public void SubscriptionLifecyclePolicy_Active_And_PastDue_Resolve_To_Full(OrganizationSubscriptionStatus status)
    {
        SubscriptionLifecycleAccess access = SubscriptionLifecyclePolicy.Evaluate(status);

        access.Mode.Should().Be(TenantAccessMode.Full);
    }

    [Theory]
    [InlineData(OrganizationSubscriptionStatus.Restricted)]
    [InlineData(OrganizationSubscriptionStatus.Expired)]
    [InlineData(OrganizationSubscriptionStatus.Canceled)]
    public void SubscriptionLifecyclePolicy_Restricted_Expired_Canceled_Resolve_To_ReadOnly_With_SourceStatus(
        OrganizationSubscriptionStatus status)
    {
        SubscriptionLifecycleAccess access = SubscriptionLifecyclePolicy.Evaluate(status);

        access.Mode.Should().Be(TenantAccessMode.ReadOnly);
        access.SourceStatus.Should().Be(status);
    }
}
