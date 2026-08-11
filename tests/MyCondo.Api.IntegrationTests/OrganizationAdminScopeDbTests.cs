using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;
using MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Phase 2 (mycondo-docs ADR-020) — OrganizationAdmin scope enforcement and tenant-boundary tests.
/// Split from a single larger test class specifically to stay well under the "auth" rate-limit policy's
/// 10/min-per-IP budget when the whole class runs against one shared PostgresApiFactory instance — same
/// reasoning as every other *DbTests split in this project (see RoleEndpointsDbTests vs
/// RolePermissionsAndAssignmentsDbTests). Needs a Docker daemon; not executed in the environment this
/// was authored in.
/// </summary>
public class OrganizationAdminScopeDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public OrganizationAdminScopeDbTests(PostgresApiFactory factory)
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

    [Fact]
    public async Task OrganizationAdmin_Assignment_Rejects_A_BuildingId()
    {
        Guid tenantId = await SeedActiveTenantAsync("orgadmin-rejects-building");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        List<RoleSummaryDto> roles = await GetRolesAsync(client, ownerTokens.AccessToken);
        Guid organizationAdminRoleId = roles.Single(r => r.Name == "OrganizationAdmin").RoleId;
        Guid buildingId = await CreateBuildingAsync(client, ownerTokens.AccessToken, "B1");

        HttpResponseMessage response = await AssignRoleAsync(
            client, ownerTokens.AccessToken, organizationAdminRoleId, memberUserId, buildingId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task OrganizationAdmin_Assignment_TenantWide_Succeeds_And_Resolves_Every_NonPlatform_Permission()
    {
        Guid tenantId = await SeedActiveTenantAsync("orgadmin-tenant-wide");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");

        JwtClaims claims = JwtTestHelper.Decode(ownerTokens.AccessToken);
        claims.ContainsClaim("tenant_id").Should().BeTrue();

        HttpResponseMessage catalogueResponse = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/permissions", ownerTokens.AccessToken);
        List<PermissionDto> catalogue = (await catalogueResponse.Content.ReadFromJsonAsync<List<PermissionDto>>(JsonOptions))!;

        // Excludes module "tenant" (tenant.view/tenant.manage) — SaaS-tenant lifecycle permissions are
        // Platform-exclusive (mycondo-docs Create Tenant audit decision), not part of what an
        // OrganizationAdmin is bootstrapped with even though /api/v1/permissions still lists them.
        List<string> grantedPermissions = claims.GetClaimValues("perm");
        grantedPermissions.Should().BeEquivalentTo(
            catalogue.Where(p => p.Module != "tenant").Select(p => p.Name));
    }

    [Fact]
    public async Task OrganizationAdmin_Cannot_Cross_Tenant_Boundary_When_Assigning_Roles()
    {
        Guid tenantAId = await SeedActiveTenantAsync("orgadmin-no-cross-tenant-a");
        Guid tenantBId = await SeedActiveTenantAsync("orgadmin-no-cross-tenant-b");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto ownerATokens = await RegisterAsync(client, tenantAId, "owner-a2@example.com");
        AuthTokensDto memberBTokens = await RegisterAsync(client, tenantBId, "member-b2@example.com");
        Guid memberBUserId = ParseUserIdFromAccessToken(memberBTokens.AccessToken);

        List<RoleSummaryDto> rolesA = await GetRolesAsync(client, ownerATokens.AccessToken);
        Guid organizationAdminRoleIdA = rolesA.Single(r => r.Name == "OrganizationAdmin").RoleId;

        HttpResponseMessage response = await AssignRoleAsync(
            client, ownerATokens.AccessToken, organizationAdminRoleIdA, memberBUserId, buildingId: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task New_Tenant_Bootstrap_Never_Creates_The_Legacy_SuperAdmin_Role()
    {
        Guid tenantId = await SeedActiveTenantAsync("no-legacy-superadmin");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");

        List<RoleSummaryDto> roles = await GetRolesAsync(client, ownerTokens.AccessToken);
        roles.Should().NotContain(r => r.Name == "SuperAdmin");
    }
}
