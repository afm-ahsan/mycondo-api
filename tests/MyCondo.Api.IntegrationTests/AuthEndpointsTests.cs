using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Covers request paths that never reach the database — unauthenticated access and validation
/// failures both short-circuit before any handler runs (authorization middleware / ValidationBehavior
/// respectively). Round-trip tests that actually need a database are in AuthEndpointsDbTests.
/// </summary>
public class AuthEndpointsTests : IClassFixture<MyCondoWebApplicationFactory>
{
    private readonly MyCondoWebApplicationFactory _factory;

    public AuthEndpointsTests(MyCondoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Me_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/logout", new { refreshToken = "whatever" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_Me_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/v1/auth/me", new { fullName = "Someone", phoneNumber = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_MyAvatar_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/auth/me/avatar");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_MyAvatar_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync("/api/v1/auth/me/avatar");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_MyAvatar_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();
        using MultipartFormDataContent form = new();
        using ByteArrayContent fileContent = new([0x89, 0x50, 0x4E, 0x47]);
        form.Add(fileContent, "file", "photo.png");

        HttpResponseMessage response = await client.PostAsync("/api/v1/auth/me/avatar", form);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "someone@example.com", "password")]
    [InlineData("00000000-0000-0000-0000-000000000000", "not-an-email", "password")]
    public async Task Login_With_Invalid_Body_Returns_400(string tenantId, string email, string password)
    {
        using HttpClient client = _factory.CreateClient();

        object body = string.IsNullOrEmpty(tenantId)
            ? new { email, password }
            : new { tenantId, email, password };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_With_Missing_TenantId_Returns_400()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new { refreshToken = "whatever" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void Auth_And_Tenant_Routes_Are_Registered()
    {
        // Was previously asserted by fetching /openapi/v1.json over HTTP, but that endpoint is now
        // Development-only (see HealthCheckTests) and this factory boots under "Testing" — inspect
        // the registered EndpointDataSource directly instead, which is environment-independent and
        // exercises the same "did the endpoint mapping actually run" concern.
        using IServiceScope scope = _factory.Services.CreateScope();
        EndpointDataSource endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();

        List<string> routePatterns = endpoints.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText ?? string.Empty)
            .ToList();

        routePatterns.Should().Contain("/api/v1/auth/login");
        routePatterns.Should().Contain("/api/v1/auth/register");
        // Only the anonymous slug lookup remains under /api/v1/tenants — Provision/Activate/Suspend
        // were removed (Create Tenant audit; see TenantEndpoints.cs's class doc comment) and now live
        // exclusively under /api/v1/platform/organizations, gated by RequirePlatformPermission.
        routePatterns.Should().Contain("/api/v1/tenants/by-slug/{slug}");
        routePatterns.Should().NotContain("/api/v1/tenants/");
    }
}
