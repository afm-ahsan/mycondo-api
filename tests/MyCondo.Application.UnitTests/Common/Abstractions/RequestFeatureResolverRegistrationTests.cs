using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.UnitTests.Common.Abstractions;

/// <summary>
/// Verifies the ADR-033 Task 09A DI wiring: every <see cref="IRequiresResolvedFeature"/>-marked request in
/// the Application assembly resolves a concrete <see cref="IRequestFeatureResolver{TRequest}"/> from
/// <see cref="DependencyInjection.AddApplication"/>'s open-generic + generic-constraint registrations, and
/// no request implements both entitlement markers at once (ADR-033 Task 09A §4 — the two categories are
/// mutually exclusive).
/// </summary>
public class RequestFeatureResolverRegistrationTests
{
    private static IReadOnlyList<Type> RequiresResolvedFeatureRequestTypes { get; } = typeof(DependencyInjection).Assembly
        .GetTypes()
        .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IRequiresResolvedFeature).IsAssignableFrom(t))
        .ToList();

    // Inspecting the ServiceCollection's descriptors (rather than building a provider and resolving)
    // confirms the registration exists without needing the repository/DbContext implementations that
    // only Infrastructure/Api register — those aren't available in an Application-only test project.
    private static readonly IServiceCollection Services = new ServiceCollection().AddApplication();

    [Fact]
    public void At_Least_One_Request_Declares_IRequiresResolvedFeature()
    {
        RequiresResolvedFeatureRequestTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void Every_IRequiresResolvedFeature_Request_Has_A_Registered_Resolver()
    {
        foreach (Type requestType in RequiresResolvedFeatureRequestTypes)
        {
            Type resolverServiceType = typeof(IRequestFeatureResolver<>).MakeGenericType(requestType);

            Services.Should().Contain(d => d.ServiceType == resolverServiceType,
                $"{requestType.FullName} implements {nameof(IRequiresResolvedFeature)} but no " +
                $"{resolverServiceType.Name} is registered for it");
        }
    }

    [Fact]
    public void No_Request_Declares_Both_IRequiresFeature_And_IRequiresResolvedFeature()
    {
        IEnumerable<Type> both = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface
                && typeof(IRequiresFeature).IsAssignableFrom(t)
                && typeof(IRequiresResolvedFeature).IsAssignableFrom(t));

        both.Should().BeEmpty();
    }
}
