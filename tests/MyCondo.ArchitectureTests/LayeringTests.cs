using AwesomeAssertions;
using NetArchTest.Rules;

namespace MyCondo.ArchitectureTests;

public class LayeringTests
{
    private const string DomainNamespace = "MyCondo.Domain";
    private const string ApplicationNamespace = "MyCondo.Application";
    private const string InfrastructureNamespace = "MyCondo.Infrastructure";
    private const string ApiNamespace = "MyCondo.Api";

    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        TestResult result = Types.InAssembly(typeof(Domain.Common.Entity<>).Assembly)
            .Should()
            .NotHaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        TestResult result = Types.InAssembly(typeof(Domain.Common.Entity<>).Assembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_EntityFrameworkCore()
    {
        TestResult result = Types.InAssembly(typeof(Domain.Common.Entity<>).Assembly)
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_AspNetCore()
    {
        TestResult result = Types.InAssembly(typeof(Domain.Common.Entity<>).Assembly)
            .Should()
            .NotHaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        TestResult result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Api()
    {
        TestResult result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        TestResult result = Types.InAssembly(typeof(Infrastructure.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(BuildFailureMessage(result));
    }

    private static string BuildFailureMessage(TestResult result) =>
        result.FailingTypes is null
            ? "Architecture rule violated."
            : $"Architecture rule violated by: {string.Join(", ", result.FailingTypes.Select(t => t.FullName))}";
}
