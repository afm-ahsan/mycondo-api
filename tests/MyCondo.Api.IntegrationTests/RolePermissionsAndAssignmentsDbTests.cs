using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Roles.Commands.CreateRole;
using MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;
using MyCondo.Application.Features.Roles.Queries.GetRoleAssignments;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests for GET /api/v1/roles/{id}/permissions and GET /api/v1/roles/{id}/assignments —
/// the read side the Role-Permission Matrix frontend screen needs (there's no other way to know a
/// role's current grants/holders). Needs a Docker daemon, same disclosed limitation as every other
/// PostgresApiFactory-backed test.
/// </summary>
public class RolePermissionsAndAssignmentsDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public RolePermissionsAndAssignmentsDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> SeedActiveTenantAsync(string slug)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Tenant tenant = Tenant.Provision($"Tenant {slug}", slug, clock.UtcNow);
        tenant.Activate(clock.UtcNow);
        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return tenant.Id.Value;
    }

    private static async Task<AuthTokensDto> RegisterAsync(HttpClient client, Guid tenantId, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email,
            password = "Correct-Horse-Battery-9",
            fullName = "Test User",
            phoneNumber = (string?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthTokensDto? tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        tokens.Should().NotBeNull();
        return tokens!;
    }

    private static async Task<HttpResponseMessage> SendAuthedAsync(
        HttpClient client, HttpMethod method, string url, string accessToken, object? body = null)
    {
        using HttpRequestMessage request = new(method, url)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static Guid ParseUserIdFromAccessToken(string accessToken)
    {
        string payload = accessToken.Split('.')[1];
        string padded = payload.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
        byte[] json = Convert.FromBase64String(padded);
        using JsonDocument document = JsonDocument.Parse(json);
        return Guid.Parse(document.RootElement.GetProperty("sub").GetString()!);
    }

    [Fact]
    public async Task Get_Role_Permissions_Returns_Only_Granted_Ones()
    {
        Guid tenantId = await SeedActiveTenantAsync("role-permissions-read");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");

        HttpResponseMessage createResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/roles", ownerTokens.AccessToken,
            new { name = "Matrix Test Role", description = "" });
        CreateRoleResult? role = await createResponse.Content.ReadFromJsonAsync<CreateRoleResult>(JsonOptions);

        HttpResponseMessage catalogueResponse = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/permissions", ownerTokens.AccessToken);
        List<PermissionDto>? catalogue = await catalogueResponse.Content.ReadFromJsonAsync<List<PermissionDto>>(JsonOptions);
        Guid permissionId = catalogue!.First(p => p.Name == "complaint.view").Id;

        (await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{role!.RoleId}/permissions", ownerTokens.AccessToken,
            new { permissionId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage grantedResponse = await SendAuthedAsync(
            client, HttpMethod.Get, $"/api/v1/roles/{role.RoleId}/permissions", ownerTokens.AccessToken);
        grantedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<PermissionDto>? granted = await grantedResponse.Content.ReadFromJsonAsync<List<PermissionDto>>(JsonOptions);

        granted.Should().ContainSingle(p => p.Name == "complaint.view");
    }

    [Fact]
    public async Task Get_Role_Assignments_Returns_Assigned_Users()
    {
        Guid tenantId = await SeedActiveTenantAsync("role-assignments-read");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        HttpResponseMessage createResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/roles", ownerTokens.AccessToken,
            new { name = "Assignment Read Role", description = "" });
        CreateRoleResult? role = await createResponse.Content.ReadFromJsonAsync<CreateRoleResult>(JsonOptions);

        (await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{role!.RoleId}/assignments", ownerTokens.AccessToken,
            new { userId = memberUserId, buildingId = (Guid?)null }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage assignmentsResponse = await SendAuthedAsync(
            client, HttpMethod.Get, $"/api/v1/roles/{role.RoleId}/assignments", ownerTokens.AccessToken);
        assignmentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RoleAssignmentDto>? assignments = await assignmentsResponse.Content.ReadFromJsonAsync<List<RoleAssignmentDto>>(JsonOptions);

        assignments.Should().ContainSingle(a => a.UserId == memberUserId && a.Email == "member@example.com");
    }
}
