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
/// Phase 3 (mycondo-docs ADR-021) — remaining two resident Tenant Role+Relationship defense-in-depth
/// DENY scenarios, split out of OccupancyAccessDbTests to stay under the "auth" rate-limit policy's
/// per-class budget (see that class's doc comment).
/// </summary>
public class OccupancyAccessPart2DbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public OccupancyAccessPart2DbTests(PostgresApiFactory factory)
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
    public async Task MovedOut_Occupancy_Denies_MyFlats_Visibility()
    {
        Guid tenantId = await SeedActiveTenantAsync("tenant-moved-out");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingId = await CreateBuildingAsync(client, adminTokens.AccessToken, "TC1");
        Guid flatId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingId, "301");
        Guid registrationResidentId = await CreateActiveOccupancyAsync(client, adminTokens.AccessToken, flatId, "Occupant Three");
        await LinkResidentToUserAsync(client, adminTokens.AccessToken, registrationResidentId, memberUserId);
        await AssignTenantRoleAsync(client, adminTokens.AccessToken, memberUserId, buildingId);

        // Find the active registration for this flat and move it out.
        HttpResponseMessage listResponse = await SendAuthedAsync(
            client, HttpMethod.Get, $"/api/v1/occupancy-registrations?flatId={flatId}&status=Active&page=1&pageSize=20", adminTokens.AccessToken);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument page = (await listResponse.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions))!;
        Guid registrationId = page.RootElement.GetProperty("items")[0].GetProperty("occupancyRegistrationId").GetGuid();

        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/occupancy-registrations/{registrationId}/move-out", adminTokens.AccessToken,
                new { reason = "Test move-out" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        List<MyFlatDto> myFlats = await GetMyFlatsAsync(client, memberLoginTokens.AccessToken);

        myFlats.Should().BeEmpty("a moved-out occupancy must stop granting access");
    }

    [Fact]
    public async Task Tenant_Role_Without_Active_Occupancy_Denies_MyFlats_Visibility()
    {
        Guid tenantId = await SeedActiveTenantAsync("tenant-no-occupancy");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingId = await CreateBuildingAsync(client, adminTokens.AccessToken, "TD1");
        // Deliberately no OccupancyRegistration/Resident link — the Tenant role holds no matching relationship.
        await AssignTenantRoleAsync(client, adminTokens.AccessToken, memberUserId, buildingId);

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        List<MyFlatDto> myFlats = await GetMyFlatsAsync(client, memberLoginTokens.AccessToken);

        myFlats.Should().BeEmpty("holding the Tenant role/permission alone must never substitute for an active occupancy relationship");
    }
}
