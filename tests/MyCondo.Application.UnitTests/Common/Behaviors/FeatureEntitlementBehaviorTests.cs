using AwesomeAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Behaviors;
using MyCondo.Application.Common.Exceptions;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Behaviors;

/// <summary>
/// Application-layer tests for <see cref="FeatureEntitlementBehavior{TMessage,TResponse}"/> (ADR-033 §16,
/// Task 06 §40; resource-derived resolution added Task 09A) — isolated from the real
/// <c>ITenantEntitlementService</c>/<c>ICurrentUserProvider</c>/resolver implementations via
/// <c>NSubstitute</c> fakes, matching <c>TenantEntitlementServiceTests</c>' convention.
/// </summary>
public class FeatureEntitlementBehaviorTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private const string FeatureKey = "security.gates";

    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly ITenantEntitlementService _entitlements = Substitute.For<ITenantEntitlementService>();
    private readonly IRequestFeatureResolver<ResolvedFeatureRequest> _resolver =
        Substitute.For<IRequestFeatureResolver<ResolvedFeatureRequest>>();

    private sealed record FeatureGatedRequest(string FeatureKey) : IRequest<string>, IRequiresFeature;

    public sealed record ResolvedFeatureRequest : IRequest<string>, IRequiresResolvedFeature;

    private sealed record PlainRequest : IRequest<string>;

    public FeatureEntitlementBehaviorTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private FeatureEntitlementBehavior<TMessage, string> CreateSut<TMessage>(IServiceProvider? services = null)
        where TMessage : IMessage =>
        new(_currentUser, _entitlements, services ?? EmptyServiceProvider());

    private static ServiceProvider EmptyServiceProvider() => new ServiceCollection().BuildServiceProvider();

    private ServiceProvider ServiceProviderWithResolver()
    {
        ServiceCollection services = new();
        services.AddSingleton(_resolver);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Passes_Through_Without_Calling_The_Resolver_When_The_Request_Has_No_Marker()
    {
        FeatureEntitlementBehavior<PlainRequest, string> sut = CreateSut<PlainRequest>();
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
        FeatureEntitlementBehavior<FeatureGatedRequest, string> sut = CreateSut<FeatureGatedRequest>();
        MessageHandlerDelegate<FeatureGatedRequest, string> next = (_, _) => new ValueTask<string>("ok");

        string result = await sut.Handle(new FeatureGatedRequest(FeatureKey), next, CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Throws_FeatureNotEntitledException_And_Does_Not_Call_Next_When_Not_Entitled()
    {
        _entitlements.IsFeatureEnabled(TenantId, FeatureKey, Arg.Any<CancellationToken>()).Returns(false);
        FeatureEntitlementBehavior<FeatureGatedRequest, string> sut = CreateSut<FeatureGatedRequest>();
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
        FeatureEntitlementBehavior<FeatureGatedRequest, string> sut = CreateSut<FeatureGatedRequest>();
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
        FeatureEntitlementBehavior<FeatureGatedRequest, string> sut = CreateSut<FeatureGatedRequest>();
        MessageHandlerDelegate<FeatureGatedRequest, string> next = (_, _) => new ValueTask<string>("ok");

        await sut.Handle(new FeatureGatedRequest(FeatureKey), next, CancellationToken.None);

        await _entitlements.Received(1).IsFeatureEnabled(TenantId, FeatureKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolved_Feature_Request_Calls_The_Registered_Resolver_And_Passes_Through_When_Entitled()
    {
        ResolvedFeatureRequest request = new();
        _resolver.ResolveFeatureKeyAsync(request, TenantId, Arg.Any<CancellationToken>()).Returns("facilities.community_hall");
        _entitlements.IsFeatureEnabled(TenantId, "facilities.community_hall", Arg.Any<CancellationToken>()).Returns(true);
        FeatureEntitlementBehavior<ResolvedFeatureRequest, string> sut =
            CreateSut<ResolvedFeatureRequest>(ServiceProviderWithResolver());
        MessageHandlerDelegate<ResolvedFeatureRequest, string> next = (_, _) => new ValueTask<string>("ok");

        string result = await sut.Handle(request, next, CancellationToken.None);

        result.Should().Be("ok");
        await _resolver.Received(1).ResolveFeatureKeyAsync(request, TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolved_Feature_Request_Throws_FeatureNotEntitledException_When_The_Resolved_Key_Is_Not_Entitled()
    {
        ResolvedFeatureRequest request = new();
        _resolver.ResolveFeatureKeyAsync(request, TenantId, Arg.Any<CancellationToken>()).Returns("utilities.gas");
        _entitlements.IsFeatureEnabled(TenantId, "utilities.gas", Arg.Any<CancellationToken>()).Returns(false);
        FeatureEntitlementBehavior<ResolvedFeatureRequest, string> sut =
            CreateSut<ResolvedFeatureRequest>(ServiceProviderWithResolver());
        bool nextCalled = false;
        MessageHandlerDelegate<ResolvedFeatureRequest, string> next = (_, _) =>
        {
            nextCalled = true;
            return new ValueTask<string>("ok");
        };

        Func<Task> act = () => sut.Handle(request, next, CancellationToken.None).AsTask();

        (await act.Should().ThrowAsync<FeatureNotEntitledException>()).Which.FeatureKey.Should().Be("utilities.gas");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Resolved_Feature_Request_Propagates_Resource_Not_Found_Instead_Of_Denying_As_Not_Entitled()
    {
        ResolvedFeatureRequest request = new();
        _resolver.ResolveFeatureKeyAsync(request, TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new NotFoundException("Booking", Guid.NewGuid())));
        FeatureEntitlementBehavior<ResolvedFeatureRequest, string> sut =
            CreateSut<ResolvedFeatureRequest>(ServiceProviderWithResolver());
        MessageHandlerDelegate<ResolvedFeatureRequest, string> next = (_, _) => new ValueTask<string>("ok");

        Func<Task> act = () => sut.Handle(request, next, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
        await _entitlements.DidNotReceive()
            .IsFeatureEnabled(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolved_Feature_Request_Throws_A_Configuration_Error_When_No_Resolver_Is_Registered()
    {
        ResolvedFeatureRequest request = new();
        FeatureEntitlementBehavior<ResolvedFeatureRequest, string> sut =
            CreateSut<ResolvedFeatureRequest>(EmptyServiceProvider());
        MessageHandlerDelegate<ResolvedFeatureRequest, string> next = (_, _) => new ValueTask<string>("ok");

        Func<Task> act = () => sut.Handle(request, next, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
