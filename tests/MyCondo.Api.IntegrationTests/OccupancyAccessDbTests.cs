using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Me.Queries.GetMyFlats;
using MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Phase 3 (mycondo-docs ADR-021) — resident Tenant role + active-occupancy relationship
/// defense-in-depth via GET /api/v1/me/flats. Reuses the existing OccupancyRegistration lifecycle as
/// the occupancy source of truth (no separate Occupancy table — see FlatAccessAuthorizer). Each test
/// here makes 2 registers + 1 login = 3 "auth" bucket calls; kept to 2 tests/class (6 calls) for
/// headroom — see OccupancyAccessPart2DbTests for the remaining two scenarios.
/// </summary>
public class OccupancyAccessDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public OccupancyAccessDbTests(PostgresApiFactory factory)
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

    /// <summary>Drives a brand-new OccupancyRegistration all the way to Active — the caller
    /// (adminAccessToken, OrganizationAdmin) holds every occupancy-registration.* permission, so one
    /// actor can play every role in the approval chain for test-setup purposes.</summary>
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

        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/occupancy-registrations/{registration.OccupancyRegistrationId}/submit", adminAccessToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/occupancy-registrations/{registration.OccupancyRegistrationId}/owner-approve", adminAccessToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/occupancy-registrations/{registration.OccupancyRegistrationId}/management-verify", adminAccessToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/occupancy-registrations/{registration.OccupancyRegistrationId}/activate", adminAccessToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        return registration.PrimaryResidentId;
    }

    private static async Task LinkResidentToUserAsync(HttpClient client, string adminAccessToken, Guid residentId, Guid userId)
    {
        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/residents/{residentId}/link-user", adminAccessToken, new { userId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task AssignTenantRoleAsync(HttpClient client, string accessToken, Guid userId, Guid buildingId)
    {
        HttpResponseMessage rolesResponse = await SendAuthedAsync(client, HttpMethod.Get, "/api/v1/roles", accessToken);
        List<RoleSummaryDto> roles = (await rolesResponse.Content.ReadFromJsonAsync<List<RoleSummaryDto>>(JsonOptions))!;
        Guid tenantRoleId = roles.Single(r => r.Name == "Tenant").RoleId;

        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/roles/{tenantRoleId}/assignments", accessToken,
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
    public async Task Resident_With_Role_And_Active_Occupancy_Sees_Occupied_Flat_Via_MyFlats()
    {
        Guid tenantId = await SeedActiveTenantAsync("tenant-access");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingId = await CreateBuildingAsync(client, adminTokens.AccessToken, "TA1");
        Guid flatId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingId, "301");
        Guid residentId = await CreateActiveOccupancyAsync(client, adminTokens.AccessToken, flatId, "Occupant One");
        await LinkResidentToUserAsync(client, adminTokens.AccessToken, residentId, memberUserId);
        await AssignTenantRoleAsync(client, adminTokens.AccessToken, memberUserId, buildingId);

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        List<MyFlatDto> myFlats = await GetMyFlatsAsync(client, memberLoginTokens.AccessToken);

        myFlats.Should().ContainSingle(f => f.FlatId == flatId && f.RelationshipType == "Occupancy");
    }

    [Fact]
    public async Task Resident_Does_Not_See_Unrelated_Flat()
    {
        Guid tenantId = await SeedActiveTenantAsync("tenant-unrelated");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingId = await CreateBuildingAsync(client, adminTokens.AccessToken, "TB1");
        Guid occupiedFlatId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingId, "301");
        Guid residentId = await CreateActiveOccupancyAsync(client, adminTokens.AccessToken, occupiedFlatId, "Occupant Two");
        await LinkResidentToUserAsync(client, adminTokens.AccessToken, residentId, memberUserId);
        await AssignTenantRoleAsync(client, adminTokens.AccessToken, memberUserId, buildingId);

        Guid unrelatedFlatId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingId, "302");

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        List<MyFlatDto> myFlats = await GetMyFlatsAsync(client, memberLoginTokens.AccessToken);

        myFlats.Should().ContainSingle(f => f.FlatId == occupiedFlatId);
        myFlats.Should().NotContain(f => f.FlatId == unrelatedFlatId);
    }
}
