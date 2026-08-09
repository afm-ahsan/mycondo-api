using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Phase 2 (mycondo-docs ADR-020) — condominium-scoped role (CondoAdmin/Manager/Accountant/
/// SecurityOfficer/FacilityManager) BuildingId scope enforcement. Split out to stay under the "auth"
/// rate-limit policy's per-class budget — see OrganizationAdminScopeDbTests's doc comment. Needs a
/// Docker daemon; not executed in the environment this was authored in.
/// </summary>
public class CondominiumRoleScopeDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public CondominiumRoleScopeDbTests(PostgresApiFactory factory)
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

    private static async Task<List<RoleSummaryDto>> GetRolesAsync(HttpClient client, string accessToken)
    {
        HttpResponseMessage response = await SendAuthedAsync(client, HttpMethod.Get, "/api/v1/roles", accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<RoleSummaryDto>>(JsonOptions))!;
    }

    private static async Task<Guid> CreateBuildingAsync(HttpClient client, string accessToken, string code)
    {
        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/properties/buildings", accessToken,
            new CreateBuildingCommand($"Building {code}", code, Address: null));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CreateBuildingResult? result = await response.Content.ReadFromJsonAsync<CreateBuildingResult>(JsonOptions);
        return result!.BuildingId;
    }

    private static Task<HttpResponseMessage> AssignRoleAsync(
        HttpClient client, string accessToken, Guid roleId, Guid userId, Guid? buildingId) =>
        SendAuthedAsync(client, HttpMethod.Post, $"/api/v1/roles/{roleId}/assignments", accessToken,
            new { userId, buildingId });

    private static async Task<AuthTokensDto> LoginAsync(HttpClient client, Guid tenantId, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { tenantId, email, password = "Correct-Horse-Battery-9" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions))!;
    }

    [Fact]
    public async Task CondoAdmin_Assignment_Without_BuildingId_Fails()
    {
        Guid tenantId = await SeedActiveTenantAsync("condoadmin-requires-building");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        List<RoleSummaryDto> roles = await GetRolesAsync(client, ownerTokens.AccessToken);
        Guid condoAdminRoleId = roles.Single(r => r.Name == "CondoAdmin").RoleId;

        HttpResponseMessage response = await AssignRoleAsync(
            client, ownerTokens.AccessToken, condoAdminRoleId, memberUserId, buildingId: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CondoAdmin_Assignment_With_Cross_Tenant_Building_Fails()
    {
        Guid tenantAId = await SeedActiveTenantAsync("condoadmin-cross-tenant-a");
        Guid tenantBId = await SeedActiveTenantAsync("condoadmin-cross-tenant-b");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto ownerATokens = await RegisterAsync(client, tenantAId, "owner-a@example.com");
        AuthTokensDto ownerBTokens = await RegisterAsync(client, tenantBId, "owner-b@example.com");
        AuthTokensDto memberATokens = await RegisterAsync(client, tenantAId, "member-a@example.com");
        Guid memberAUserId = ParseUserIdFromAccessToken(memberATokens.AccessToken);

        Guid buildingBId = await CreateBuildingAsync(client, ownerBTokens.AccessToken, "B-CROSS");

        List<RoleSummaryDto> rolesA = await GetRolesAsync(client, ownerATokens.AccessToken);
        Guid condoAdminRoleId = rolesA.Single(r => r.Name == "CondoAdmin").RoleId;

        HttpResponseMessage response = await AssignRoleAsync(
            client, ownerATokens.AccessToken, condoAdminRoleId, memberAUserId, buildingBId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CondoAdmin_Permissions_Apply_Only_To_The_Assigned_Building()
    {
        Guid tenantId = await SeedActiveTenantAsync("condoadmin-building-scoped-perms");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingAId = await CreateBuildingAsync(client, ownerTokens.AccessToken, "BA");
        Guid buildingBId = await CreateBuildingAsync(client, ownerTokens.AccessToken, "BB");

        List<RoleSummaryDto> roles = await GetRolesAsync(client, ownerTokens.AccessToken);
        Guid condoAdminRoleId = roles.Single(r => r.Name == "CondoAdmin").RoleId;

        (await AssignRoleAsync(client, ownerTokens.AccessToken, condoAdminRoleId, memberUserId, buildingAId))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        JwtClaims claims = JwtTestHelper.Decode(memberLoginTokens.AccessToken);

        claims.GetClaimValues("building_ids").Should().ContainSingle(id => id == buildingAId.ToString());
        List<string> buildingPermissions = claims.GetClaimValues("bperm");
        buildingPermissions.Should().Contain($"{buildingAId}|property.view");
        buildingPermissions.Should().NotContain(p => p.StartsWith($"{buildingBId}|", StringComparison.Ordinal));
        claims.GetClaimValues("perm").Should().BeEmpty("CondoAdmin is building-scoped, never tenant-wide");
    }
}
