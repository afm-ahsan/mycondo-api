using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;
using MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForFlat;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Application.Features.Residents.Commands.CreateResident;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Phase 3 final pre-commit review (mycondo-docs ADR-021, ADR-020) — CondoAdmin's ownership.manage
/// grant is Building-scoped (`bperm`), unlike OrganizationAdmin's tenant-wide grant, so the
/// FlatOwnership admin endpoints must check HasPermissionForBuilding per-Flat's-Building rather than a
/// single tenant-wide RequirePermission (which a Building-scoped grant can never satisfy). Verifies
/// both that CondoAdmin can manage ownership for their own Building and cannot for another.
/// </summary>
public class FlatOwnershipAdminBuildingScopeDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public FlatOwnershipAdminBuildingScopeDbTests(PostgresApiFactory factory)
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

    private static async Task<Guid> CreateResidentAsync(HttpClient client, string accessToken, Guid flatId, string fullName)
    {
        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/residents", accessToken,
            new CreateResidentCommand(flatId, fullName, Phone: null, Email: null, ResidentType: "Owner"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ResidentDto? result = await response.Content.ReadFromJsonAsync<ResidentDto>(JsonOptions);
        return result!.ResidentId;
    }

    private static async Task AssignCondoAdminRoleAsync(HttpClient client, string accessToken, Guid userId, Guid buildingId)
    {
        HttpResponseMessage rolesResponse = await SendAuthedAsync(client, HttpMethod.Get, "/api/v1/roles", accessToken);
        List<RoleSummaryDto> roles = (await rolesResponse.Content.ReadFromJsonAsync<List<RoleSummaryDto>>(JsonOptions))!;
        Guid condoAdminRoleId = roles.Single(r => r.Name == "CondoAdmin").RoleId;

        (await SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/roles/{condoAdminRoleId}/assignments", accessToken,
                new { userId, buildingId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CondoAdmin_Can_Manage_Ownership_For_Own_Building_But_Not_Another()
    {
        Guid tenantId = await SeedActiveTenantAsync("condoadmin-ownership-scope");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto orgAdminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto condoAdminTokens = await RegisterAsync(client, tenantId, "condo-admin@example.com");
        Guid condoAdminUserId = ParseUserIdFromAccessToken(condoAdminTokens.AccessToken);

        Guid buildingAId = await CreateBuildingAsync(client, orgAdminTokens.AccessToken, "CS1");
        Guid buildingBId = await CreateBuildingAsync(client, orgAdminTokens.AccessToken, "CS2");
        Guid flatAId = await CreateFlatAsync(client, orgAdminTokens.AccessToken, buildingAId, "101");
        Guid flatBId = await CreateFlatAsync(client, orgAdminTokens.AccessToken, buildingBId, "201");

        // FlatOwnership references a Resident party record, not a portal User directly — create one
        // per Flat (org admin's tenant-wide grants avoid this test depending on Resident permission
        // scoping, which is not what this test is verifying).
        Guid residentAId = await CreateResidentAsync(client, orgAdminTokens.AccessToken, flatAId, "Owner A");
        Guid residentBId = await CreateResidentAsync(client, orgAdminTokens.AccessToken, flatBId, "Owner B");

        await AssignCondoAdminRoleAsync(client, orgAdminTokens.AccessToken, condoAdminUserId, buildingAId);
        AuthTokensDto condoAdminFreshTokens = await LoginAsync(client, tenantId, "condo-admin@example.com");

        // Own Building (A): create, list, and end an ownership all succeed.
        HttpResponseMessage createInOwnBuilding = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/properties/flat-ownerships", condoAdminFreshTokens.AccessToken,
            new CreateFlatOwnershipCommand(residentAId, flatAId, DateOnly.FromDateTime(DateTime.UtcNow)));
        createInOwnBuilding.StatusCode.Should().Be(HttpStatusCode.OK);
        CreateFlatOwnershipResult ownershipInOwnBuilding =
            (await createInOwnBuilding.Content.ReadFromJsonAsync<CreateFlatOwnershipResult>(JsonOptions))!;

        (await SendAuthedAsync(
                client, HttpMethod.Get, $"/api/v1/properties/flats/{flatAId}/ownerships", condoAdminFreshTokens.AccessToken))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await SendAuthedAsync(
                client, HttpMethod.Delete,
                $"/api/v1/properties/flat-ownerships/{ownershipInOwnBuilding.FlatOwnershipId}?endDate={DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}",
                condoAdminFreshTokens.AccessToken))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Another Building (B): create and list are both denied.
        HttpResponseMessage createInOtherBuilding = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/properties/flat-ownerships", condoAdminFreshTokens.AccessToken,
            new CreateFlatOwnershipCommand(residentBId, flatBId, DateOnly.FromDateTime(DateTime.UtcNow)));
        createInOtherBuilding.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await SendAuthedAsync(
                client, HttpMethod.Get, $"/api/v1/properties/flats/{flatBId}/ownerships", condoAdminFreshTokens.AccessToken))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
