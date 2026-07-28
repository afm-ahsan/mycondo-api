using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Covers request paths that never reach the database — unauthenticated access to every
/// role/permission-management endpoint. Round-trip tests that actually exercise the SuperAdmin
/// bootstrap and permission catalogue are in RoleEndpointsDbTests.
/// </summary>
public class RoleEndpointsTests : IClassFixture<MyCondoWebApplicationFactory>
{
    private readonly MyCondoWebApplicationFactory _factory;

    public RoleEndpointsTests(MyCondoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Role_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/roles", new { name = "Building Manager", description = "Ops" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Grant_Permission_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/roles/{Guid.NewGuid()}/permissions", new { permissionId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Assign_Role_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/roles/{Guid.NewGuid()}/assignments", new { userId = Guid.NewGuid(), buildingId = (Guid?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivate_Role_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/roles/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Remove_Permission_From_Role_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/v1/roles/{Guid.NewGuid()}/permissions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_Role_From_User_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync(
            $"/api/v1/roles/{Guid.NewGuid()}/assignments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Roles_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Permissions_Without_Token_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OpenApi_Document_Includes_Role_Routes()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");
        string document = await response.Content.ReadAsStringAsync();

        document.Should().Contain("/api/v1/roles");
        document.Should().Contain("/api/v1/permissions");
    }
}
