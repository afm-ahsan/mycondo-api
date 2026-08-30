using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Behaviors;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Behaviors;

/// <summary>
/// Application-layer tests for <see cref="TenantLifecycleBehavior{TMessage,TResponse}"/> (ADR-032 §6,
/// Task 10 §8/§9/§31/§32) — isolated from the real <c>ICurrentUserProvider</c>/
/// <c>ITenantLifecycleAccessService</c>/<c>ISubscriptionLifecycleAccessService</c> via NSubstitute fakes,
/// matching <c>FeatureEntitlementBehaviorTests</c>' convention. Covers the pipeline-composition matrix
/// (ADR-032 Task 10 §81/§82): org dominance, PastDue full access, Restricted read/write split, and the
/// fail-closed default for unmarked requests.
/// </summary>
public class TenantLifecycleBehaviorTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly ITenantLifecycleAccessService _organizationLifecycle = Substitute.For<ITenantLifecycleAccessService>();
    private readonly ISubscriptionLifecycleAccessService _subscriptionLifecycle = Substitute.For<ISubscriptionLifecycleAccessService>();

    private sealed record PlainRequest : IRequest<string>;
    private sealed record ReadRequest : IRequest<string>, ILifecycleReadOperation;
    private sealed record WriteRequest : IRequest<string>, ILifecycleWriteOperation;
    private sealed record BillingResolutionRequest : IRequest<string>, IBillingResolutionOperation;

    public TenantLifecycleBehaviorTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _organizationLifecycle.GetTenantStatusAsync(TenantId, Arg.Any<CancellationToken>()).Returns(TenantStatus.Active);
        _subscriptionLifecycle.GetAccessAsync(TenantId, Arg.Any<CancellationToken>()).Returns(SubscriptionLifecycleAccess.Full);
    }

    private TenantLifecycleBehavior<TMessage, string> CreateSut<TMessage>() where TMessage : IMessage =>
        new(_currentUser, _organizationLifecycle, _subscriptionLifecycle);

    private static MessageHandlerDelegate<TMessage, string> Next<TMessage>(Action? onCalled = null) where TMessage : IMessage =>
        (_, _) =>
        {
            onCalled?.Invoke();
            return new ValueTask<string>("ok");
        };

    [Fact]
    public async Task Passes_Through_Without_Any_Lookup_When_There_Is_No_Tenant_Context()
    {
        _currentUser.TenantId.Returns((Guid?)null);
        TenantLifecycleBehavior<PlainRequest, string> sut = CreateSut<PlainRequest>();

        string result = await sut.Handle(new PlainRequest(), Next<PlainRequest>(), CancellationToken.None);

        result.Should().Be("ok");
        await _organizationLifecycle.DidNotReceive().GetTenantStatusAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _subscriptionLifecycle.DidNotReceive().GetAccessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Active_Organization_And_Active_Subscription_Allows_An_Unmarked_Request()
    {
        TenantLifecycleBehavior<PlainRequest, string> sut = CreateSut<PlainRequest>();

        string result = await sut.Handle(new PlainRequest(), Next<PlainRequest>(), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Theory]
    [InlineData(TenantStatus.Suspended)]
    [InlineData(TenantStatus.Closed)]
    public async Task Organization_Lockout_Denies_Access_Regardless_Of_Subscription_Or_Read_Write(TenantStatus status)
    {
        // Task 10 §69: organization dominance — even a Full subscription and a read-marked request must
        // not bypass an organization-level hard lockout.
        _organizationLifecycle.GetTenantStatusAsync(TenantId, Arg.Any<CancellationToken>()).Returns(status);
        TenantLifecycleBehavior<ReadRequest, string> sut = CreateSut<ReadRequest>();
        bool nextCalled = false;

        Func<Task> act = () => sut.Handle(new ReadRequest(), Next<ReadRequest>(() => nextCalled = true), CancellationToken.None).AsTask();

        (await act.Should().ThrowAsync<OrganizationLifecycleAccessDeniedException>()).Which.Status.Should().Be(status);
        nextCalled.Should().BeFalse();
        await _subscriptionLifecycle.DidNotReceive().GetAccessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PastDue_Subscription_Allows_An_Unmarked_Write_Request()
    {
        // Task 10 §62 — PastDue must retain full access; it must never be treated as ReadOnly.
        _subscriptionLifecycle.GetAccessAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionLifecycleAccess.Full);
        TenantLifecycleBehavior<WriteRequest, string> sut = CreateSut<WriteRequest>();

        string result = await sut.Handle(new WriteRequest(), Next<WriteRequest>(), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Theory]
    [InlineData(OrganizationSubscriptionStatus.Restricted)]
    [InlineData(OrganizationSubscriptionStatus.Expired)]
    [InlineData(OrganizationSubscriptionStatus.Canceled)]
    public async Task ReadOnly_Subscription_Allows_A_Read_Marked_Request(OrganizationSubscriptionStatus sourceStatus)
    {
        _subscriptionLifecycle.GetAccessAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionLifecycleAccess.ReadOnly(sourceStatus));
        TenantLifecycleBehavior<ReadRequest, string> sut = CreateSut<ReadRequest>();

        string result = await sut.Handle(new ReadRequest(), Next<ReadRequest>(), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Theory]
    [InlineData(OrganizationSubscriptionStatus.Restricted)]
    [InlineData(OrganizationSubscriptionStatus.Expired)]
    [InlineData(OrganizationSubscriptionStatus.Canceled)]
    public async Task ReadOnly_Subscription_Denies_A_Write_Marked_Request(OrganizationSubscriptionStatus sourceStatus)
    {
        _subscriptionLifecycle.GetAccessAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionLifecycleAccess.ReadOnly(sourceStatus));
        TenantLifecycleBehavior<WriteRequest, string> sut = CreateSut<WriteRequest>();
        bool nextCalled = false;

        Func<Task> act = () => sut.Handle(new WriteRequest(), Next<WriteRequest>(() => nextCalled = true), CancellationToken.None).AsTask();

        (await act.Should().ThrowAsync<SubscriptionLifecycleAccessDeniedException>()).Which.SubscriptionStatus.Should().Be(sourceStatus);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ReadOnly_Subscription_Denies_An_Unmarked_Request_By_Default_Fail_Closed()
    {
        // ADR-032 Task 10 §59 — a request with no lifecycle marker at all is treated as a blocked write,
        // not a permitted read, since the pilot rollout does not cover every handler yet.
        _subscriptionLifecycle.GetAccessAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionLifecycleAccess.ReadOnly(OrganizationSubscriptionStatus.Restricted));
        TenantLifecycleBehavior<PlainRequest, string> sut = CreateSut<PlainRequest>();

        Func<Task> act = () => sut.Handle(new PlainRequest(), Next<PlainRequest>(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<SubscriptionLifecycleAccessDeniedException>();
    }

    [Fact]
    public async Task ReadOnly_Subscription_Allows_A_BillingResolution_Marked_Request()
    {
        _subscriptionLifecycle.GetAccessAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(SubscriptionLifecycleAccess.ReadOnly(OrganizationSubscriptionStatus.Expired));
        TenantLifecycleBehavior<BillingResolutionRequest, string> sut = CreateSut<BillingResolutionRequest>();

        string result = await sut.Handle(new BillingResolutionRequest(), Next<BillingResolutionRequest>(), CancellationToken.None);

        result.Should().Be("ok");
    }
}
