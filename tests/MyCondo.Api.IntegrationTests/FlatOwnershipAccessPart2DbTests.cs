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
using MyCondo.Application.Features.Residents.Commands.CreateResident;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Phase 3 (mycondo-docs ADR-021) — remaining two FlatOwner Role+Relationship defense-in-depth DENY
/// scenarios, split out of FlatOwnershipAccessDbTests to stay under the "auth" rate-limit policy's
/// per-class budget (see that class's doc comment).
/// </summary>
public class FlatOwnershipAccessPart2DbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public FlatOwnershipAccessPart2DbTests(PostgresApiFactory factory)
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

    /// <summary>Creates a Resident for the Flat, links it to the given portal User, and grants it
    /// ownership — FlatOwnership references a Resident, and self-service "My Flats" resolves a
    /// logged-in User's ownership via Residents bridged to that User (Resident.UserId).</summary>
    private static async Task<Guid> GrantOwnershipAsync(HttpClient client, string accessToken, Guid userId, Guid flatId, string ownerName)
    {
        HttpResponseMessage residentResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/residents", accessToken,
            new CreateResidentCommand(flatId, ownerName, Phone: null, Email: null, ResidentType: "Owner"));
        residentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ResidentDto resident = (await residentResponse.Content.ReadFromJsonAsync<ResidentDto>(JsonOptions))!;

        (await SendAuthedAsync(
                client, HttpMethod.Post, $"/api/v1/residents/{resident.ResidentId}/link-user", accessToken, new { userId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/properties/flat-ownerships", accessToken,
            new CreateFlatOwnershipCommand(resident.ResidentId, flatId, DateOnly.FromDateTime(DateTime.UtcNow)));
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
    public async Task Ownership_Without_FlatOwner_Role_Denies_MyFlats_Visibility()
    {
        Guid tenantId = await SeedActiveTenantAsync("flatowner-no-role");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingId = await CreateBuildingAsync(client, adminTokens.AccessToken, "FC1");
        Guid flatId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingId, "101");
        await GrantOwnershipAsync(client, adminTokens.AccessToken, memberUserId, flatId, "Owner Member");
        // Deliberately no FlatOwner role assignment.

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        List<MyFlatDto> myFlats = await GetMyFlatsAsync(client, memberLoginTokens.AccessToken);

        myFlats.Should().BeEmpty("an active ownership relationship alone must never substitute for the FlatOwner role/permission");
    }

    [Fact]
    public async Task FlatOwner_Role_Without_Ownership_Denies_MyFlats_Visibility()
    {
        Guid tenantId = await SeedActiveTenantAsync("flatowner-no-relationship");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingId = await CreateBuildingAsync(client, adminTokens.AccessToken, "FD1");
        // Deliberately no FlatOwnership row created for this Flat.
        await AssignFlatOwnerRoleAsync(client, adminTokens.AccessToken, memberUserId, buildingId);

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        List<MyFlatDto> myFlats = await GetMyFlatsAsync(client, memberLoginTokens.AccessToken);

        myFlats.Should().BeEmpty("holding the FlatOwner role/permission alone must never substitute for an actual ownership relationship");
    }
}
