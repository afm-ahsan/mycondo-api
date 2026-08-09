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
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Phase 3 (mycondo-docs ADR-021) — admin (OrganizationAdmin/CondoAdmin, ownership.manage) relationship
/// management: create/end FlatOwnership, and the cross-tenant/duplicate-active-relationship rejections
/// that must fail before persistence (mycondo-docs ADR-021 §12 relationship-consistency requirement).
/// Split into its own class for the "auth" rate-limit policy's per-class budget — see
/// OrganizationAdminScopeDbTests's doc comment (Phase 2). Needs a Docker daemon; not executed in the
/// environment this was authored in.
/// </summary>
public class FlatOwnershipAdminManagementDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public FlatOwnershipAdminManagementDbTests(PostgresApiFactory factory)
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

    [Fact]
    public async Task Admin_Can_Create_And_End_Ownership()
    {
        Guid tenantId = await SeedActiveTenantAsync("ownership-admin-crud");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingId = await CreateBuildingAsync(client, adminTokens.AccessToken, "AM1");
        Guid flatId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingId, "101");

        HttpResponseMessage createResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/properties/flat-ownerships", adminTokens.AccessToken,
            new CreateFlatOwnershipCommand(memberUserId, flatId, DateOnly.FromDateTime(DateTime.UtcNow)));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        CreateFlatOwnershipResult ownership = (await createResponse.Content.ReadFromJsonAsync<CreateFlatOwnershipResult>(JsonOptions))!;

        (await SendAuthedAsync(
                client, HttpMethod.Delete,
                $"/api/v1/properties/flat-ownerships/{ownership.FlatOwnershipId}?endDate={DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}",
                adminTokens.AccessToken))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage listResponse = await SendAuthedAsync(
            client, HttpMethod.Get, $"/api/v1/properties/flats/{flatId}/ownerships", adminTokens.AccessToken);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<FlatOwnershipDto> ownerships = (await listResponse.Content.ReadFromJsonAsync<List<FlatOwnershipDto>>(JsonOptions))!;

        ownerships.Should().ContainSingle(o => o.FlatOwnershipId == ownership.FlatOwnershipId && o.Status == "Ended");
    }

    [Fact]
    public async Task Cannot_Bind_A_User_From_One_Tenant_To_A_Flat_In_Another_Tenant()
    {
        Guid tenantAId = await SeedActiveTenantAsync("ownership-cross-tenant-a");
        Guid tenantBId = await SeedActiveTenantAsync("ownership-cross-tenant-b");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto adminATokens = await RegisterAsync(client, tenantAId, "admin-a@example.com");
        AuthTokensDto adminBTokens = await RegisterAsync(client, tenantBId, "admin-b@example.com");
        AuthTokensDto memberBTokens = await RegisterAsync(client, tenantBId, "member-b@example.com");
        Guid memberBUserId = ParseUserIdFromAccessToken(memberBTokens.AccessToken);

        Guid buildingAId = await CreateBuildingAsync(client, adminATokens.AccessToken, "CX1");
        Guid flatAId = await CreateFlatAsync(client, adminATokens.AccessToken, buildingAId, "101");

        // Tenant A's admin tries to grant ownership of Tenant A's flat to a Tenant B user.
        HttpResponseMessage crossTenantResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/properties/flat-ownerships", adminATokens.AccessToken,
            new CreateFlatOwnershipCommand(memberBUserId, flatAId, DateOnly.FromDateTime(DateTime.UtcNow)));
        crossTenantResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Also verify the reverse: Tenant B's admin cannot grant ownership of Tenant A's flat either.
        HttpResponseMessage crossTenantFlatResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/properties/flat-ownerships", adminBTokens.AccessToken,
            new CreateFlatOwnershipCommand(memberBUserId, flatAId, DateOnly.FromDateTime(DateTime.UtcNow)));
        crossTenantFlatResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Duplicate_Active_Ownership_For_The_Same_User_And_Flat_Is_Rejected()
    {
        Guid tenantId = await SeedActiveTenantAsync("ownership-duplicate");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto adminTokens = await RegisterAsync(client, tenantId, "admin@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingId = await CreateBuildingAsync(client, adminTokens.AccessToken, "DP1");
        Guid flatId = await CreateFlatAsync(client, adminTokens.AccessToken, buildingId, "101");

        CreateFlatOwnershipCommand command = new(memberUserId, flatId, DateOnly.FromDateTime(DateTime.UtcNow));
        (await SendAuthedAsync(client, HttpMethod.Post, "/api/v1/properties/flat-ownerships", adminTokens.AccessToken, command))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage duplicateResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/properties/flat-ownerships", adminTokens.AccessToken, command);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
