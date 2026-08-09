using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Me.Queries.GetMyFlats;
using MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Phase 3 (mycondo-docs ADR-021) — a schema that never assumed "one Flat = one owner forever" or
/// "one User = one Flat": co-ownership/multi-Flat ownership come for free from FlatOwnership just
/// allowing more than one active row, and a User can be a FlatOwner in one Flat and a resident Tenant
/// in another simultaneously. Split into its own class for the "auth" rate-limit policy's per-class
/// budget — see OrganizationAdminScopeDbTests's doc comment (Phase 2). Needs a Docker daemon; not
/// executed in the environment this was authored in.
/// </summary>
public class MixedFlatRelationshipDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public MixedFlatRelationshipDbTests(PostgresApiFactory factory)
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

    private static async Task GrantOwnershipAsync(HttpClient client, string accessToken, Guid userId, Guid flatId)
    {
        (await SendAuthedAsync(
                client, HttpMethod.Post, "/api/v1/properties/flat-ownerships", accessToken,
                new CreateFlatOwnershipCommand(userId, flatId, DateOnly.FromDateTime(DateTime.UtcNow))))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task AssignRoleAsync(HttpClient client, string accessToken, string roleName, Guid userId, Guid buildingId)
    {
        HttpResponseMessage rolesResponse = await SendAuthedAsync(client, HttpMethod.Get, "/api/v1/roles", accessToken);
        List<RoleSummaryDto> roles = (await rolesResponse.Content.ReadFromJsonAsync<List<RoleSummaryDto>>(JsonOptions))!;
        Guid roleId = roles.Single(r => r.Name == roleName).RoleId;

        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/roles/{roleId}/assignments", accessToken,
                new { userId, buildingId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<Guid> CreateActiveOccupancyAsync(HttpClient client, string adminAccessToken, Guid flatId, string primaryFullName)
    {
        HttpResponseMessage createResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/occupancy-registrations", adminAccessToken,
            new
            {
                flatId,
                occupancyType = "Occupant",
                primaryFullName,
                primaryPhone = (string?)null,
                primaryEmail = (string?)null,
                primaryNationalIdNumber = (string?)null,
                primaryDateOfBirth = (DateOnly?)null,
                primaryPermanentAddress = (string?)null,
                emergencyContactName = (string?)null,
                emergencyContactPhone = (string?)null,
                moveInExpectedDate = (DateOnly?)null,
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        OccupancyRegistrationDto registration = (await createResponse.Content.ReadFromJsonAsync<OccupancyRegistrationDto>(JsonOptions))!;

        foreach (string step in new[] { "submit", "owner-approve", "management-verify", "activate" })
        {
            (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/occupancy-registrations/{registration.OccupancyRegistrationId}/{step}", adminAccessToken))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }

        return registration.PrimaryResidentId;
    }

    private static async Task<List<MyFlatDto>> GetMyFlatsAsync(HttpClient client, string accessToken)
    {
        HttpResponseMessage response = await SendAuthedAsync(client, HttpMethod.Get, "/api/v1/me/flats", accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<MyFlatDto>>(JsonOptions))!;
    }

    [Fact]
    public async Task User_May_Own_Multiple_Flats_Across_Different_Buildings()
    {
        Guid tenantId = await SeedActiveTenantAsync("mixed-multi-flat-owner");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingAId = await CreateBuildingAsync(client, adminTokens.AccessToken, "MA1");
        Guid flatAId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingAId, "101");
        await GrantOwnershipAsync(client, adminTokens.AccessToken, memberUserId, flatAId);
        await AssignRoleAsync(client, adminTokens.AccessToken, "FlatOwner", memberUserId, buildingAId);

        Guid buildingBId = await CreateBuildingAsync(client, adminTokens.AccessToken, "MA2");
        Guid flatBId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingBId, "201");
        await GrantOwnershipAsync(client, adminTokens.AccessToken, memberUserId, flatBId);
        await AssignRoleAsync(client, adminTokens.AccessToken, "FlatOwner", memberUserId, buildingBId);

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        List<MyFlatDto> myFlats = await GetMyFlatsAsync(client, memberLoginTokens.AccessToken);

        myFlats.Should().Contain(f => f.FlatId == flatAId && f.RelationshipType == "Ownership");
        myFlats.Should().Contain(f => f.FlatId == flatBId && f.RelationshipType == "Ownership");
        myFlats.Should().HaveCount(2);
    }

    [Fact]
    public async Task User_May_Own_One_Flat_And_Occupy_Another_Simultaneously()
    {
        Guid tenantId = await SeedActiveTenantAsync("mixed-owner-and-tenant");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid ownedBuildingId = await CreateBuildingAsync(client, adminTokens.AccessToken, "MB1");
        Guid ownedFlatId = await CreateFlatAsync(client, adminTokens.AccessToken, ownedBuildingId, "101");
        await GrantOwnershipAsync(client, adminTokens.AccessToken, memberUserId, ownedFlatId);
        await AssignRoleAsync(client, adminTokens.AccessToken, "FlatOwner", memberUserId, ownedBuildingId);

        Guid occupiedBuildingId = await CreateBuildingAsync(client, adminTokens.AccessToken, "MB2");
        Guid occupiedFlatId = await CreateFlatAsync(client, adminTokens.AccessToken, occupiedBuildingId, "201");
        Guid residentId = await CreateActiveOccupancyAsync(client, adminTokens.AccessToken, occupiedFlatId, "Mixed Occupant");
        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/residents/{residentId}/link-user", adminTokens.AccessToken, new { userId = memberUserId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        await AssignRoleAsync(client, adminTokens.AccessToken, "Tenant", memberUserId, occupiedBuildingId);

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        List<MyFlatDto> myFlats = await GetMyFlatsAsync(client, memberLoginTokens.AccessToken);

        myFlats.Should().Contain(f => f.FlatId == ownedFlatId && f.RelationshipType == "Ownership");
        myFlats.Should().Contain(f => f.FlatId == occupiedFlatId && f.RelationshipType == "Occupancy");
        myFlats.Should().HaveCount(2);
    }
}
