using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;

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
    public async Task Provision_Tenant_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/tenants", new { name = "ARP", slug = "arp" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OpenApi_Document_Includes_New_Routes()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");
        string document = await response.Content.ReadAsStringAsync();

        document.Should().Contain("/api/v1/auth/login");
        document.Should().Contain("/api/v1/auth/register");
        document.Should().Contain("/api/v1/tenants");
    }
}
