using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Property.Buildings.Commands.CreateBuilding;
using MyCondo.Application.Features.Roles.Queries.GetRoleAssignments;
using MyCondo.Application.Features.Roles.Queries.GetRolesForTenant;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Phase 2 (mycondo-docs ADR-020) — multiple role assignments, revocation, and delegation/
/// privilege-escalation boundaries. Split out to stay under the "auth" rate-limit policy's per-class
/// budget — see OrganizationAdminScopeDbTests's doc comment. Needs a Docker daemon; not executed in the
/// environment this was authored in.
/// </summary>
public class CondominiumRoleDelegationDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public CondominiumRoleDelegationDbTests(PostgresApiFactory factory)
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
    public async Task User_May_Hold_Multiple_Condominium_Roles_In_Different_Buildings()
    {
        Guid tenantId = await SeedActiveTenantAsync("multi-role-multi-building");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingAId = await CreateBuildingAsync(client, ownerTokens.AccessToken, "MA");
        Guid buildingBId = await CreateBuildingAsync(client, ownerTokens.AccessToken, "MB");

        List<RoleSummaryDto> roles = await GetRolesAsync(client, ownerTokens.AccessToken);
        Guid condoAdminRoleId = roles.Single(r => r.Name == "CondoAdmin").RoleId;
        Guid managerRoleId = roles.Single(r => r.Name == "Manager").RoleId;

        (await AssignRoleAsync(client, ownerTokens.AccessToken, condoAdminRoleId, memberUserId, buildingAId))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await AssignRoleAsync(client, ownerTokens.AccessToken, managerRoleId, memberUserId, buildingBId))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");
        JwtClaims claims = JwtTestHelper.Decode(memberLoginTokens.AccessToken);

        claims.GetClaimValues("building_ids").Should().BeEquivalentTo([buildingAId.ToString(), buildingBId.ToString()]);
        List<string> buildingPermissions = claims.GetClaimValues("bperm");
        buildingPermissions.Should().Contain($"{buildingAId}|property.view"); // CondoAdmin@A
        buildingPermissions.Should().Contain($"{buildingBId}|report.operational.view"); // Manager@B
        buildingPermissions.Should().NotContain($"{buildingAId}|report.operational.view", "Manager was only granted for Building B");
        buildingPermissions.Should().NotContain($"{buildingBId}|property.view", "CondoAdmin was only granted for Building A");
    }

    [Fact]
    public async Task Revoking_One_Scoped_Assignment_Does_Not_Remove_The_Other()
    {
        Guid tenantId = await SeedActiveTenantAsync("revoke-one-of-two");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingAId = await CreateBuildingAsync(client, ownerTokens.AccessToken, "RA");
        Guid buildingBId = await CreateBuildingAsync(client, ownerTokens.AccessToken, "RB");

        List<RoleSummaryDto> roles = await GetRolesAsync(client, ownerTokens.AccessToken);
        Guid condoAdminRoleId = roles.Single(r => r.Name == "CondoAdmin").RoleId;

        (await AssignRoleAsync(client, ownerTokens.AccessToken, condoAdminRoleId, memberUserId, buildingAId))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await AssignRoleAsync(client, ownerTokens.AccessToken, condoAdminRoleId, memberUserId, buildingBId))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await SendAuthedAsync(
            client, HttpMethod.Delete, $"/api/v1/roles/{condoAdminRoleId}/assignments/{memberUserId}?buildingId={buildingAId}",
            ownerTokens.AccessToken))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage assignmentsResponse = await SendAuthedAsync(
            client, HttpMethod.Get, $"/api/v1/roles/{condoAdminRoleId}/assignments", ownerTokens.AccessToken);
        List<RoleAssignmentDto> assignments = (await assignmentsResponse.Content.ReadFromJsonAsync<List<RoleAssignmentDto>>(JsonOptions))!;

        assignments.Should().ContainSingle(a => a.UserId == memberUserId && a.BuildingId == buildingBId);
        assignments.Should().NotContain(a => a.UserId == memberUserId && a.BuildingId == buildingAId);
    }

    [Fact]
    public async Task OrganizationAdmin_Cannot_Grant_A_Platform_Permission_To_A_Tenant_Role()
    {
        Guid tenantId = await SeedActiveTenantAsync("orgadmin-no-platform-grant");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        Permission platformPermission = await db.Set<Permission>().AsNoTracking().FirstAsync(p => p.Module == "platform");

        List<RoleSummaryDto> roles = await GetRolesAsync(client, ownerTokens.AccessToken);
        Guid organizationAdminRoleId = roles.Single(r => r.Name == "OrganizationAdmin").RoleId;

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{organizationAdminRoleId}/permissions", ownerTokens.AccessToken,
            new { permissionId = platformPermission.Id.Value });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Custom_Role_Permission_Escalation_To_Platform_Scope_Is_Rejected()
    {
        // Same guard as the system-role case above, exercised against an ordinary tenant-admin-created
        // custom role (POST /api/v1/roles) — the block in GrantPermissionToRoleCommandHandler applies
        // uniformly regardless of Role.IsSystem, so a tenant admin can't work around it by creating
        // their own role instead of using OrganizationAdmin.
        Guid tenantId = await SeedActiveTenantAsync("custom-role-no-platform-grant");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        Permission platformPermission = await db.Set<Permission>().AsNoTracking().FirstAsync(p => p.Module == "platform");

        HttpResponseMessage createResponse = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/roles", ownerTokens.AccessToken,
            new { name = "Custom Escalation Attempt", description = "" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid customRoleId = (await createResponse.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions))!
            .RootElement.GetProperty("roleId").GetGuid();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{customRoleId}/permissions", ownerTokens.AccessToken,
            new { permissionId = platformPermission.Id.Value });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CondoAdmin_Has_No_Role_Management_Authority()
    {
        Guid tenantId = await SeedActiveTenantAsync("condoadmin-no-role-manage");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerTokens = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto memberTokens = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberUserId = ParseUserIdFromAccessToken(memberTokens.AccessToken);

        Guid buildingId = await CreateBuildingAsync(client, ownerTokens.AccessToken, "DEL");
        List<RoleSummaryDto> roles = await GetRolesAsync(client, ownerTokens.AccessToken);
        Guid condoAdminRoleId = roles.Single(r => r.Name == "CondoAdmin").RoleId;

        (await AssignRoleAsync(client, ownerTokens.AccessToken, condoAdminRoleId, memberUserId, buildingId))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        AuthTokensDto memberLoginTokens = await LoginAsync(client, tenantId, "member@example.com");

        // Neither creating a new role nor assigning/granting on an existing one — CondoAdmin (and every
        // other condominium-scoped role) is deliberately granted no role.manage/role.view in Phase 2
        // (mycondo-docs ADR-020's delegation-boundary design decision): role administration remains
        // OrganizationAdmin's exclusive, tenant-wide capability.
        (await SendAuthedAsync(client, HttpMethod.Post, "/api/v1/roles", memberLoginTokens.AccessToken,
                new { name = "Should Not Be Creatable", description = "" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await AssignRoleAsync(client, memberLoginTokens.AccessToken, condoAdminRoleId, memberUserId, buildingId))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
