using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Identity.RolePermissions;
using MyCondo.Domain.Features.Identity.Roles;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory), proving
/// the OrganizationAdmin bootstrap (RegisterUserCommandHandler) and the permission catalogue end-to-end.
/// These need a Docker daemon and were NOT executed in the environment they were authored in — see
/// PostgresApiFactory's doc comment. Run wherever Docker is available before trusting them.
/// </summary>
public class RoleEndpointsDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public RoleEndpointsDbTests(PostgresApiFactory factory)
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

    [Fact]
    public async Task First_User_Of_Tenant_Is_Bootstrapped_As_OrganizationAdmin()
    {
        Guid tenantId = await SeedActiveTenantAsync("orgadmin-bootstrap");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto tokens = await RegisterAsync(client, tenantId, "first-user@example.com");

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/roles");
        request.Headers.Authorization = new("Bearer", tokens.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<RoleSummaryDto>? roles = await response.Content.ReadFromJsonAsync<List<RoleSummaryDto>>(JsonOptions);
        roles.Should().ContainSingle(r =>
            r.Name == "OrganizationAdmin" && r.IsSystem && r.Code == "organization.admin" && r.RequiresBuildingScope == false);
    }

    [Fact]
    public async Task Second_User_Of_Tenant_Is_Not_Bootstrapped_And_Cannot_View_Roles()
    {
        Guid tenantId = await SeedActiveTenantAsync("superadmin-second-user");
        using HttpClient client = _factory.CreateClient();

        await RegisterAsync(client, tenantId, "first-user@example.com");
        AuthTokensDto secondUserTokens = await RegisterAsync(client, tenantId, "second-user@example.com");

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/roles");
        request.Headers.Authorization = new("Bearer", secondUserTokens.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Bootstrapped_User_Can_Create_A_New_Role()
    {
        Guid tenantId = await SeedActiveTenantAsync("superadmin-create-role");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto tokens = await RegisterAsync(client, tenantId, "owner@example.com");

        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/roles")
        {
            Content = JsonContent.Create(new { name = "Building Manager", description = "Ops" }),
        };
        request.Headers.Authorization = new("Bearer", tokens.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Tenant_Permission_Catalogue_Excludes_Platform_Permissions()
    {
        // Formerly "Get_Permissions_Returns_Full_Seeded_Catalogue", asserting a hardcoded count (99)
        // that was already stale before Phase 2 (the real non-platform count had grown to 132 across
        // several later slices) and, since Phase 2, is architecturally the wrong thing to assert at
        // all: GetPermissionCatalogueQueryHandler now deliberately filters platform.* out of this
        // endpoint's response (mycondo-docs ADR-020) — a fixed magic number can never distinguish
        // "the catalogue grew" from "the platform.* exclusion broke," so this test now asserts the
        // actual invariant Phase 2 introduced instead of an incidental count.
        Guid tenantId = await SeedActiveTenantAsync("permission-catalogue");
        using HttpClient client = _factory.CreateClient();

        AuthTokensDto tokens = await RegisterAsync(client, tenantId, "owner@example.com");

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/permissions");
        request.Headers.Authorization = new("Bearer", tokens.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<PermissionDto>? permissions = await response.Content.ReadFromJsonAsync<List<PermissionDto>>(JsonOptions);

        permissions.Should().NotBeNullOrEmpty();
        permissions!.Should().OnlyContain(p => p.Module != "platform", "tenant RBAC must never receive or expose platform.* permissions");
        permissions.Select(p => p.Name).Should().OnlyHaveUniqueItems();

        // Representative permissions across several long-standing tenant modules remain present —
        // proves the exclusion filter is scoped to "platform" specifically, not an over-broad filter
        // that accidentally also dropped real tenant permissions.
        permissions.Should().Contain(p => p.Name == "role.manage");
        permissions.Should().Contain(p => p.Name == "permission.view");
        permissions.Should().Contain(p => p.Name == "billing.invoice.generate");
        permissions.Should().Contain(p => p.Name == "utility.reading.correct");

        // The endpoint's result must equal the database's actual non-platform catalogue exactly — not
        // a hardcoded number, so this never goes stale as the catalogue grows in later slices.
        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        int expectedNonPlatformCount = await db.Set<Permission>().CountAsync(p => p.Module != "platform");
        permissions.Should().HaveCount(expectedNonPlatformCount);
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

    [Fact]
    public async Task Deactivate_Role_Removes_It_From_The_Role_List()
    {
        Guid tenantId = await SeedActiveTenantAsync("deactivate-role");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, "owner@example.com");

        HttpResponseMessage createResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/roles", tokens.AccessToken,
            new { name = "Temp Role", description = "To be deactivated" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid roleId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roleId").GetGuid();

        HttpResponseMessage deactivateResponse = await SendAuthedAsync(
            client, HttpMethod.Delete, $"/api/v1/roles/{roleId}", tokens.AccessToken);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage listResponse = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/roles", tokens.AccessToken);
        List<RoleSummaryDto>? roles = await listResponse.Content.ReadFromJsonAsync<List<RoleSummaryDto>>(JsonOptions);
        roles.Should().NotContain(r => r.RoleId == roleId);
    }

    [Fact]
    public async Task Remove_Permission_From_Role_Allows_Re_Granting_It()
    {
        Guid tenantId = await SeedActiveTenantAsync("remove-permission");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, "owner@example.com");

        HttpResponseMessage createResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/roles", tokens.AccessToken,
            new { name = "Permission Test Role", description = "" });
        Guid roleId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roleId").GetGuid();

        HttpResponseMessage catalogueResponse = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/permissions", tokens.AccessToken);
        List<PermissionDto>? catalogue = await catalogueResponse.Content.ReadFromJsonAsync<List<PermissionDto>>(JsonOptions);
        Guid permissionId = catalogue!.First(p => p.Name == "complaint.view").Id;

        HttpResponseMessage grantResponse = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{roleId}/permissions", tokens.AccessToken,
            new { permissionId });
        grantResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage regrantConflict = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{roleId}/permissions", tokens.AccessToken,
            new { permissionId });
        regrantConflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        HttpResponseMessage removeResponse = await SendAuthedAsync(
            client, HttpMethod.Delete, $"/api/v1/roles/{roleId}/permissions/{permissionId}", tokens.AccessToken);
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage regrantAfterRemove = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{roleId}/permissions", tokens.AccessToken,
            new { permissionId });
        regrantAfterRemove.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Revoke_Role_From_User_Allows_Re_Assigning_It()
    {
        Guid tenantId = await SeedActiveTenantAsync("revoke-assignment");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");

        HttpResponseMessage createResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/roles", ownerTokens.AccessToken,
            new { name = "Assignment Test Role", description = "" });
        Guid roleId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roleId").GetGuid();

        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        HttpResponseMessage assignResponse = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{roleId}/assignments", ownerTokens.AccessToken,
            new { userId = memberUserId, buildingId = (Guid?)null });
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage reassignConflict = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{roleId}/assignments", ownerTokens.AccessToken,
            new { userId = memberUserId, buildingId = (Guid?)null });
        reassignConflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        HttpResponseMessage revokeResponse = await SendAuthedAsync(
            client, HttpMethod.Delete, $"/api/v1/roles/{roleId}/assignments/{memberUserId}", ownerTokens.AccessToken);
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage reassignAfterRevoke = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{roleId}/assignments", ownerTokens.AccessToken,
            new { userId = memberUserId, buildingId = (Guid?)null });
        reassignAfterRevoke.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task First_User_Registration_Seeds_The_Default_Role_Catalogue()
    {
        Guid tenantId = await SeedActiveTenantAsync("default-role-catalogue");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, "owner@example.com");

        HttpResponseMessage listResponse = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/roles", tokens.AccessToken);
        List<RoleSummaryDto>? roleList = await listResponse.Content.ReadFromJsonAsync<List<RoleSummaryDto>>(JsonOptions);

        roleList.Should().NotBeNull();
        // OrganizationAdmin (Phase 2, mycondo-docs ADR-020) + the 7 ROLE_CATALOGUE_PROPOSAL.md custom
        // roles + the 5 Phase-2 condominium-scoped system roles + the 2 Phase-3 resident-facing system
        // roles (mycondo-docs ADR-021).
        roleList!.Select(r => r.Name).Should().BeEquivalentTo(
        [
            "OrganizationAdmin", "BuildingAdmin", "Treasurer", "Secretary", "SecurityHead", "Owner", "Renter", "Auditor",
            "CondoAdmin", "Manager", "Accountant", "SecurityOfficer", "FacilityManager",
            "FlatOwner", "Tenant",
        ]);
        roleList.Should().OnlyContain(r =>
            r.Name == "OrganizationAdmin" || r.Name == "CondoAdmin" || r.Name == "Manager"
            || r.Name == "Accountant" || r.Name == "SecurityOfficer" || r.Name == "FacilityManager"
            || r.Name == "FlatOwner" || r.Name == "Tenant"
            || !r.IsSystem);

        RoleSummaryDto organizationAdmin = roleList.Single(r => r.Name == "OrganizationAdmin");
        organizationAdmin.RequiresBuildingScope.Should().BeFalse();
        organizationAdmin.Code.Should().Be("organization.admin");

        foreach ((string name, string code) in new[]
        {
            ("CondoAdmin", "condominium.admin"), ("Manager", "condominium.manager"),
            ("Accountant", "condominium.accountant"), ("SecurityOfficer", "condominium.security"),
            ("FacilityManager", "condominium.facility-manager"),
            ("FlatOwner", "resident.flat-owner"), ("Tenant", "resident.tenant"),
        })
        {
            RoleSummaryDto condoRole = roleList.Single(r => r.Name == name);
            condoRole.RequiresBuildingScope.Should().BeTrue();
            condoRole.Code.Should().Be(code);
        }

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);

        async Task<int> GrantCountAsync(string roleName)
        {
            RoleId roleId = new(roleList.Single(r => r.Name == roleName).RoleId);
            return await db.Set<RolePermission>().CountAsync(rp => rp.RoleId == roleId);
        }

        (await GrantCountAsync("SecurityHead")).Should().Be(1);
        (await GrantCountAsync("Treasurer")).Should().Be(11);
        (await GrantCountAsync("Auditor")).Should().Be(18);
        (await GrantCountAsync("CondoAdmin")).Should().Be(28);
        (await GrantCountAsync("Manager")).Should().Be(9);
        (await GrantCountAsync("Accountant")).Should().Be(8);
        (await GrantCountAsync("SecurityOfficer")).Should().Be(19);
        (await GrantCountAsync("FacilityManager")).Should().Be(19);
        (await GrantCountAsync("FlatOwner")).Should().Be(2);
        (await GrantCountAsync("Tenant")).Should().Be(2);

        // OrganizationAdmin gets the entire tenant-usable catalogue — never a hardcoded number, since
        // it must always equal whatever /api/v1/permissions actually returns (platform.* excluded).
        HttpResponseMessage catalogueResponse = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/permissions", tokens.AccessToken);
        List<PermissionDto>? catalogue = await catalogueResponse.Content.ReadFromJsonAsync<List<PermissionDto>>(JsonOptions);
        catalogue.Should().NotBeNull();
        catalogue!.Should().OnlyContain(p => p.Module != "platform");
        (await GrantCountAsync("OrganizationAdmin")).Should().Be(catalogue.Count);
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
}
