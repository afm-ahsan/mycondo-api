using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Me.Queries.GetMyFlats;
using MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Phase 3 (mycondo-docs ADR-021) — FlatOwner role + FlatOwnership relationship defense-in-depth via
/// GET /api/v1/me/flats. Split into its own class to stay under the "auth" rate-limit policy's
/// per-class budget — see OrganizationAdminScopeDbTests's doc comment (Phase 2). Each test here makes
/// 2 registers + 1 login = 3 "auth" bucket calls; kept to 2 tests/class (6 calls) for headroom — see
/// FlatOwnershipAccessPart2DbTests for the remaining two scenarios.
/// </summary>
public class FlatOwnershipAccessDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public FlatOwnershipAccessDbTests(PostgresApiFactory factory)
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

    private static async Task<AuthTokensDto> LoginAsync(HttpClient client, Guid tenantId, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { tenantId, email, password = "Correct-Horse-Battery-9" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions))!;
    }

    private static async Task<HttpResponseMessage> SendAuthedAsync(
        HttpClient client, HttpMethod method, string url, string accessToken, object? body = null)
    {
        using HttpRequestMessage request = new(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static Guid ParseUserIdFromAccessToken(string accessToken) =>
        Guid.Parse(JwtTestHelper.Decode(accessToken).GetClaimValue("sub")!);

    private static async Task<Guid> CreateBuildingAsync(HttpClient client, string accessToken, string code)
    {
        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/properties/buildings", accessToken,
            new CreateBuildingCommand($"Building {code}", code, Address: null));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CreateBuildingResult? result = await response.Content.ReadFromJsonAsync<CreateBuildingResult>(JsonOptions);
        return result!.BuildingId;
    }

    private static async Task<Guid> CreateFlatAsync(HttpClient client, string accessToken, Guid buildingId, string flatNumber)
    {
        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/properties/buildings/{buildingId}/flats", accessToken,
            new { flatNumber, floorNumber = (int?)null, flatType = "Residential" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        FlatDto? result = await response.Content.ReadFromJsonAsync<FlatDto>(JsonOptions);
        return result!.FlatId;
    }

    private static async Task<Guid> GrantOwnershipAsync(HttpClient client, string accessToken, Guid userId, Guid flatId)
    {
        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/properties/flat-ownerships", accessToken,
            new CreateFlatOwnershipCommand(userId, flatId, DateOnly.FromDateTime(DateTime.UtcNow)));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CreateFlatOwnershipResult? result = await response.Content.ReadFromJsonAsync<CreateFlatOwnershipResult>(JsonOptions);
        return result!.FlatOwnershipId;
    }

    private static async Task AssignFlatOwnerRoleAsync(HttpClient client, string accessToken, Guid userId, Guid buildingId)
    {
        HttpResponseMessage rolesResponse = await SendAuthedAsync(client, HttpMethod.Get, "/api/v1/roles", accessToken);
        List<RoleSummaryDto> roles = (await rolesResponse.Content.ReadFromJsonAsync<List<RoleSummaryDto>>(JsonOptions))!;
        Guid flatOwnerRoleId = roles.Single(r => r.Name == "FlatOwner").RoleId;

        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/roles/{flatOwnerRoleId}/assignments", accessToken,
                new { userId, buildingId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<List<MyFlatDto>> GetMyFlatsAsync(HttpClient client, string accessToken)
    {
        HttpResponseMessage response = await SendAuthedAsync(client, HttpMethod.Get, "/api/v1/me/flats", accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<MyFlatDto>>(JsonOptions))!;
    }

    [Fact]
    public async Task Owner_With_Role_And_Ownership_Sees_Owned_Flat_Via_MyFlats()
    {
        Guid tenantId = await SeedActiveTenantAsync("flatowner-access");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerAdminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingId = await CreateBuildingAsync(client, ownerAdminTokens.AccessToken, "FA1");
        Guid flatId = await CreateFlatAsync(client, ownerAdminTokens.AccessToken, buildingId, "101");
        await GrantOwnershipAsync(client, ownerAdminTokens.AccessToken, memberUserId, flatId);
        await AssignFlatOwnerRoleAsync(client, ownerAdminTokens.AccessToken, memberUserId, buildingId);

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        List<MyFlatDto> myFlats = await GetMyFlatsAsync(client, memberLoginTokens.AccessToken);

        myFlats.Should().ContainSingle(f => f.FlatId == flatId && f.RelationshipType == "Ownership");
    }

    [Fact]
    public async Task Owner_Does_Not_See_Unrelated_Flat_In_Different_Building()
    {
        Guid tenantId = await SeedActiveTenantAsync("flatowner-unrelated");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingAId = await CreateBuildingAsync(client, adminTokens.AccessToken, "FB1");
        Guid ownedFlatId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingAId, "101");
        await GrantOwnershipAsync(client, adminTokens.AccessToken, memberUserId, ownedFlatId);
        await AssignFlatOwnerRoleAsync(client, adminTokens.AccessToken, memberUserId, buildingAId);

        Guid buildingBId = await CreateBuildingAsync(client, adminTokens.AccessToken, "FB2");
        Guid unrelatedFlatId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingBId, "201");

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        List<MyFlatDto> myFlats = await GetMyFlatsAsync(client, memberLoginTokens.AccessToken);

        myFlats.Should().ContainSingle(f => f.FlatId == ownedFlatId);
        myFlats.Should().NotContain(f => f.FlatId == unrelatedFlatId);
    }
}
