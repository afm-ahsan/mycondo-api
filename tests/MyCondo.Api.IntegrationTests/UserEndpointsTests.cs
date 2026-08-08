using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Covers request paths that never reach the database — unauthenticated access to the user
/// management endpoints. Round-trip tests that actually exercise the queries/commands are in
/// UserEndpointsDbTests.
/// </summary>
public class UserEndpointsTests : IClassFixture<MyCondoWebApplicationFactory>
{
    private readonly MyCondoWebApplicationFactory _factory;

    public UserEndpointsTests(MyCondoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Users_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Disable_User_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1/users/{Guid.NewGuid()}/disable", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void User_Routes_Are_Registered()
    {
        // Was previously asserted by fetching /openapi/v1.json over HTTP, but that endpoint is now
        // Development-only (see HealthCheckTests) and this factory boots under "Testing" — inspect
        // the registered EndpointDataSource directly instead (see AuthEndpointsTests for the same
        // pattern).
        using IServiceScope scope = _factory.Services.CreateScope();
        EndpointDataSource endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();

        List<string> routePatterns = endpoints.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText ?? string.Empty)
            .ToList();

        routePatterns.Should().Contain("/api/v1/users/");
    }
}
