using AwesomeAssertions;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Behaviors;
using MyCondo.Application.Common.Exceptions;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Behaviors;

/// <summary>
/// Application-layer tests for <see cref="FeatureEntitlementBehavior{TMessage,TResponse}"/> (ADR-033 §16,
/// Task 06 §40) — isolated from the real <c>ITenantEntitlementService</c>/<c>ICurrentUserProvider</c>
/// implementations via <c>NSubstitute</c> fakes, matching <c>TenantEntitlementServiceTests</c>' convention.
/// </summary>
public class FeatureEntitlementBehaviorTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private const string FeatureKey = "security.gates";

    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly ITenantEntitlementService _entitlements = Substitute.For<ITenantEntitlementService>();

    private sealed record FeatureGatedRequest(string FeatureKey) : IRequest<string>, IRequiresFeature;

    private sealed record PlainRequest : IRequest<string>;

    public FeatureEntitlementBehaviorTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    [Fact]
    public async Task Passes_Through_Without_Calling_The_Resolver_When_The_Request_Has_No_Marker()
    {
        FeatureEntitlementBehavior<PlainRequest, string> sut = new(_currentUser, _entitlements);
        MessageHandlerDelegate<PlainRequest, string> next = (_, _) => new ValueTask<string>("ok");

        string result = await sut.Handle(new PlainRequest(), next, CancellationToken.None);

        result.Should().Be("ok");
        await _entitlements.DidNotReceive()
            .IsFeatureEnabled(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Calls_Next_When_The_Feature_Is_Entitled()
    {
        _entitlements.IsFeatureEnabled(TenantId, FeatureKey, Arg.Any<CancellationToken>()).Returns(true);
        FeatureEntitlementBehavior<FeatureGatedRequest, string> sut = new(_currentUser, _entitlements);
        MessageHandlerDelegate<FeatureGatedRequest, string> next = (_, _) => new ValueTask<string>("ok");

        string result = await sut.Handle(new FeatureGatedRequest(FeatureKey), next, CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Throws_FeatureNotEntitledException_And_Does_Not_Call_Next_When_Not_Entitled()
    {
        _entitlements.IsFeatureEnabled(TenantId, FeatureKey, Arg.Any<CancellationToken>()).Returns(false);
        FeatureEntitlementBehavior<FeatureGatedRequest, string> sut = new(_currentUser, _entitlements);
        bool nextCalled = false;
        MessageHandlerDelegate<FeatureGatedRequest, string> next = (_, _) =>
        {
            nextCalled = true;
            return new ValueTask<string>("ok");
        };

        Func<Task> act = () => sut.Handle(new FeatureGatedRequest(FeatureKey), next, CancellationToken.None).AsTask();

        (await act.Should().ThrowAsync<FeatureNotEntitledException>()).Which.FeatureKey.Should().Be(FeatureKey);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Throws_When_The_Request_Requires_A_Feature_But_No_Tenant_Context_Is_Present()
    {
        _currentUser.TenantId.Returns((Guid?)null);
        FeatureEntitlementBehavior<FeatureGatedRequest, string> sut = new(_currentUser, _entitlements);
        MessageHandlerDelegate<FeatureGatedRequest, string> next = (_, _) => new ValueTask<string>("ok");

        Func<Task> act = () => sut.Handle(new FeatureGatedRequest(FeatureKey), next, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _entitlements.DidNotReceive()
            .IsFeatureEnabled(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Calls_The_Resolver_Exactly_Once_With_The_Correct_Tenant_And_Feature_Key()
    {
        _entitlements.IsFeatureEnabled(TenantId, FeatureKey, Arg.Any<CancellationToken>()).Returns(true);
        FeatureEntitlementBehavior<FeatureGatedRequest, string> sut = new(_currentUser, _entitlements);
        MessageHandlerDelegate<FeatureGatedRequest, string> next = (_, _) => new ValueTask<string>("ok");

        await sut.Handle(new FeatureGatedRequest(FeatureKey), next, CancellationToken.None);

        await _entitlements.Received(1).IsFeatureEnabled(TenantId, FeatureKey, Arg.Any<CancellationToken>());
    }
}
