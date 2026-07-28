using AwesomeAssertions;
using NetArchTest.Rules;

namespace MyCondo.ArchitectureTests;

/// <summary>
/// Enforces the mandatory feature-first layout: every Command/Query in MyCondo.Application must live
/// under a Features/&lt;Feature&gt; namespace, never in a flat top-level or shared-root folder. This
/// is the exact rule that would have caught the original "Users/Commands/..." layout found in Wave 0.
/// </summary>
public class FeatureFolderConventionTests
{
    [Fact]
    public void Commands_Should_Reside_Under_Application_Features_Namespace()
    {
        TestResult result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .That()
            .ResideInNamespaceContaining(".Commands.")
            .Should()
            .ResideInNamespaceStartingWith("MyCondo.Application.Features.")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Queries_Should_Reside_Under_Application_Features_Namespace()
    {
        TestResult result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .That()
            .ResideInNamespaceContaining(".Queries.")
            .Should()
            .ResideInNamespaceStartingWith("MyCondo.Application.Features.")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void No_Application_Types_Should_Reside_In_A_Shared_Root_Commands_Or_Queries_Folder()
    {
        // Guards against the anti-pattern the strategy document explicitly prohibits: a top-level
        // MyCondo.Application.Commands / MyCondo.Application.Queries dumping ground that isn't
        // nested under any feature.
        IEnumerable<Type> offendingTypes = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .That()
            .ResideInNamespace("MyCondo.Application.Commands")
            .Or()
            .ResideInNamespace("MyCondo.Application.Queries")
            .GetTypes();

        offendingTypes.Should().BeEmpty(
            "Commands/Queries must live under a Features/<Feature> namespace, never a shared root folder");
    }

    [Fact]
    public void Command_Handlers_Should_Have_Handler_Suffix()
    {
        TestResult result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .That()
            .ResideInNamespaceContaining(".Commands.")
            .And()
            .ImplementInterface(typeof(Mediator.IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    private static string BuildFailureMessage(TestResult result) =>
        result.FailingTypes is null
            ? "Feature-folder convention violated."
            : $"Feature-folder convention violated by: {string.Join(", ", result.FailingTypes.Select(t => t.FullName))}";
}
