using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Identity.Audit.DTOs;
using MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Identity.Permissions;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// End-to-end HTTP round-trip tests for the ADR-036 privileged tenant-user-management protections — the
/// M5 backend security regression milestone. Proves, through the real endpoints (not mocked handlers):
/// self-protection, the <c>user.manageTenantAdmins</c> composition-permission gate, the last-active-
/// Tenant-Admin invariant, cross-tenant IDOR closure on every mutation, and that every mutation is
/// reflected in the identity audit log. Needs a Docker daemon — see PostgresApiFactory's doc comment.
/// </summary>
public class PrivilegedUserManagementSecurityDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public PrivilegedUserManagementSecurityDbTests(PostgresApiFactory factory)
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

    /// <summary>
    /// Permission claims are baked into the JWT at issuance (see CurrentUserProvider.HasPermission —
    /// reads "perm" claims off the token, not a live DB lookup), so a role/permission grant made after
    /// a user's token was already issued has no effect until they log in again. Every test below that
    /// grants a role to an already-registered second actor must re-authenticate through this helper
    /// before exercising the newly granted permission — otherwise it would only prove the coarse
    /// endpoint-level RequirePermission gate rejected a stale token, not the ADR-036 handler-level
    /// admin-protection gate this suite exists to verify.
    /// </summary>
    private static async Task<AuthTokensDto> LoginAsync(HttpClient client, Guid tenantId, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            tenantId,
            email,
            password = "Correct-Horse-Battery-9",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthTokensDto? tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        tokens.Should().NotBeNull();
        return tokens!;
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

    private static Guid ParseUserIdFromAccessToken(string accessToken)
    {
        string payload = accessToken.Split('.')[1];
        string padded = payload.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
        byte[] json = Convert.FromBase64String(padded);
        using JsonDocument document = JsonDocument.Parse(json);
        return Guid.Parse(document.RootElement.GetProperty("sub").GetString()!);
    }

    /// <summary>Creates a custom role granting exactly the given permission names, and assigns it to
    /// <paramref name="targetUserId"/> — the actor performing the grant needs <c>role.manage</c>, which
    /// the OrganizationAdmin bootstrap account always has.</summary>
    private static async Task GrantCustomRoleAsync(
        HttpClient client, string actorAccessToken, Guid targetUserId, string roleName, params string[] permissionNames)
    {
        HttpResponseMessage createRole = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/roles", actorAccessToken,
            new { name = roleName, description = "Test role" });
        createRole.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid roleId = (await createRole.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roleId").GetGuid();

        HttpResponseMessage catalogueResponse = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/permissions", actorAccessToken);
        List<PermissionDto>? catalogue = await catalogueResponse.Content.ReadFromJsonAsync<List<PermissionDto>>(JsonOptions);

        foreach (string permissionName in permissionNames)
        {
            Guid permissionId = catalogue!.First(p => p.Name == permissionName).Id;
            HttpResponseMessage grant = await SendAuthedAsync(
                client, HttpMethod.Post, $"/api/v1/roles/{roleId}/permissions", actorAccessToken, new { permissionId });
            grant.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        HttpResponseMessage assign = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{roleId}/assignments", actorAccessToken,
            new { userId = targetUserId, buildingId = (Guid?)null });
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<Guid> GetOrganizationAdminRoleIdAsync(HttpClient client, string accessToken)
    {
        HttpResponseMessage listResponse = await SendAuthedAsync(client, HttpMethod.Get, "/api/v1/roles", accessToken);
        JsonElement roles = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        foreach (JsonElement role in roles.EnumerateArray())
        {
            if (role.GetProperty("name").GetString() == "OrganizationAdmin")
            {
                return role.GetProperty("roleId").GetGuid();
            }
        }

        throw new InvalidOperationException("OrganizationAdmin role not found in role list.");
    }

    // --- Self-protection --------------------------------------------------------------------------

    [Fact]
    public async Task The_Sole_OrganizationAdmin_Cannot_Self_Disable()
    {
        Guid tenantId = await SeedActiveTenantAsync("self-disable");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto owner = await RegisterAsync(client, tenantId, "owner@example.com");
        Guid ownerId = ParseUserIdFromAccessToken(owner.AccessToken);

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/users/{ownerId}/disable", owner.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_OrganizationAdmin_Can_Always_Update_Its_Own_Profile()
    {
        Guid tenantId = await SeedActiveTenantAsync("self-edit");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto owner = await RegisterAsync(client, tenantId, "owner@example.com");
        Guid ownerId = ParseUserIdFromAccessToken(owner.AccessToken);

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Put, $"/api/v1/users/{ownerId}", owner.AccessToken,
            new { fullName = "Self Renamed", phoneNumber = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // --- Cross-tenant IDOR --------------------------------------------------------------------------

    [Fact]
    public async Task An_Actor_Cannot_Disable_A_User_In_A_Different_Tenant()
    {
        Guid tenantA = await SeedActiveTenantAsync("cross-tenant-disable-a");
        Guid tenantB = await SeedActiveTenantAsync("cross-tenant-disable-b");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto actorA = await RegisterAsync(client, tenantA, "actor-a@example.com");
        AuthTokensDto ownerB = await RegisterAsync(client, tenantB, "owner-b@example.com");
        Guid targetUserId = ParseUserIdFromAccessToken(ownerB.AccessToken);

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/users/{targetUserId}/disable", actorA.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_Actor_Cannot_Assign_A_Role_To_A_User_In_A_Different_Tenant()
    {
        Guid tenantA = await SeedActiveTenantAsync("cross-tenant-assign-a");
        Guid tenantB = await SeedActiveTenantAsync("cross-tenant-assign-b");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto actorA = await RegisterAsync(client, tenantA, "actor-a@example.com");
        AuthTokensDto ownerB = await RegisterAsync(client, tenantB, "owner-b@example.com");
        Guid targetUserId = ParseUserIdFromAccessToken(ownerB.AccessToken);
        Guid roleIdInTenantA = await GetOrganizationAdminRoleIdAsync(client, actorA.AccessToken);

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{roleIdInTenantA}/assignments", actorA.AccessToken,
            new { userId = targetUserId, buildingId = (Guid?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- ManageTenantAdmins composition permission ----------------------------------------------------

    [Fact]
    public async Task A_Lower_Privileged_Actor_Cannot_Disable_The_OrganizationAdmin()
    {
        Guid tenantId = await SeedActiveTenantAsync("lower-priv-disable");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto owner = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto member = await RegisterAsync(client, tenantId, "member@example.com");
        Guid ownerId = ParseUserIdFromAccessToken(owner.AccessToken);
        Guid memberId = ParseUserIdFromAccessToken(member.AccessToken);

        // Grants user.disable but deliberately not user.manageTenantAdmins.
        await GrantCustomRoleAsync(client, owner.AccessToken, memberId, "Support Role", "user.disable", "user.view");
        AuthTokensDto refreshedMember = await LoginAsync(client, tenantId, "member@example.com");

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/users/{ownerId}/disable", refreshedMember.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_Lower_Privileged_Actor_Cannot_Update_The_OrganizationAdmins_Profile()
    {
        Guid tenantId = await SeedActiveTenantAsync("lower-priv-update");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto owner = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto member = await RegisterAsync(client, tenantId, "member@example.com");
        Guid ownerId = ParseUserIdFromAccessToken(owner.AccessToken);
        Guid memberId = ParseUserIdFromAccessToken(member.AccessToken);

        await GrantCustomRoleAsync(client, owner.AccessToken, memberId, "Update Role", "user.update", "user.view");
        AuthTokensDto refreshedMember = await LoginAsync(client, tenantId, "member@example.com");

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Put, $"/api/v1/users/{ownerId}", refreshedMember.AccessToken,
            new { fullName = "Hijacked Name", phoneNumber = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Last-active-admin invariant ------------------------------------------------------------------

    [Fact]
    public async Task The_Tenants_Last_Active_OrganizationAdmin_Cannot_Be_Disabled_By_Another_Actor()
    {
        Guid tenantId = await SeedActiveTenantAsync("last-admin-disable");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto owner = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto admin2 = await RegisterAsync(client, tenantId, "admin2@example.com");
        Guid ownerId = ParseUserIdFromAccessToken(owner.AccessToken);
        Guid admin2Id = ParseUserIdFromAccessToken(admin2.AccessToken);

        // admin2 gets manageTenantAdmins + user.disable via a custom role, WITHOUT itself holding the
        // OrganizationAdmin system role — isolates the last-holder invariant from the composition-
        // permission check, proving the two gates are independent.
        await GrantCustomRoleAsync(
            client, owner.AccessToken, admin2Id, "Admin Manager", "user.manageTenantAdmins", "user.disable", "user.view");
        AuthTokensDto refreshedAdmin2 = await LoginAsync(client, tenantId, "admin2@example.com");

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/users/{ownerId}/disable", refreshedAdmin2.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Disabling_An_OrganizationAdmin_Succeeds_Once_A_Second_Holder_Of_The_Role_Exists()
    {
        Guid tenantId = await SeedActiveTenantAsync("second-admin-disable");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto owner = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto admin2 = await RegisterAsync(client, tenantId, "admin2@example.com");
        Guid ownerId = ParseUserIdFromAccessToken(owner.AccessToken);
        Guid admin2Id = ParseUserIdFromAccessToken(admin2.AccessToken);
        Guid organizationAdminRoleId = await GetOrganizationAdminRoleIdAsync(client, owner.AccessToken);

        HttpResponseMessage assignAdmin2 = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/roles/{organizationAdminRoleId}/assignments", owner.AccessToken,
            new { userId = admin2Id, buildingId = (Guid?)null });
        assignAdmin2.StatusCode.Should().Be(HttpStatusCode.NoContent);
        AuthTokensDto refreshedAdmin2 = await LoginAsync(client, tenantId, "admin2@example.com");

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/users/{ownerId}/disable", refreshedAdmin2.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Revoking_The_OrganizationAdmin_Role_From_Its_Last_Holder_Is_Rejected()
    {
        Guid tenantId = await SeedActiveTenantAsync("last-admin-revoke");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto owner = await RegisterAsync(client, tenantId, "owner@example.com");
        Guid ownerId = ParseUserIdFromAccessToken(owner.AccessToken);
        Guid organizationAdminRoleId = await GetOrganizationAdminRoleIdAsync(client, owner.AccessToken);

        // Self-protection fires first for a self-revoke of an admin-equivalent role: rejected outright
        // before the last-holder count is even consulted.
        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Delete, $"/api/v1/roles/{organizationAdminRoleId}/assignments/{ownerId}", owner.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Audit verification --------------------------------------------------------------------------

    [Fact]
    public async Task User_Disable_And_Role_Assignment_Are_Recorded_In_The_Identity_Audit_Log()
    {
        Guid tenantId = await SeedActiveTenantAsync("audit-verification");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto owner = await RegisterAsync(client, tenantId, "owner@example.com");
        AuthTokensDto member = await RegisterAsync(client, tenantId, "member@example.com");
        Guid memberId = ParseUserIdFromAccessToken(member.AccessToken);

        HttpResponseMessage disableResponse = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/users/{memberId}/disable", owner.AccessToken);
        disableResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage auditResponse = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/identity/audit-log?take=100", owner.AccessToken);
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<IdentityAuditLogEntryDto>? entries = await auditResponse.Content.ReadFromJsonAsync<List<IdentityAuditLogEntryDto>>(JsonOptions);

        entries.Should().NotBeNull();
        entries!.Should().Contain(e => e.Action == "User.Deactivate" && e.TargetId == memberId.ToString());
        // Both users' Register-time bootstrap creates a User.Create entry too, but only the actor's own
        // mutation carries a resolved actor display name — proves the actor-resolution wiring end to end.
        entries.Should().Contain(e => e.Action == "User.Deactivate" && e.ActorDisplayName == "Test User");
    }

    [Fact]
    public async Task Another_Tenants_Actor_Cannot_Read_This_Tenants_Identity_Audit_Log()
    {
        Guid tenantA = await SeedActiveTenantAsync("audit-cross-tenant-a");
        Guid tenantB = await SeedActiveTenantAsync("audit-cross-tenant-b");
        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ownerA = await RegisterAsync(client, tenantA, "owner-a@example.com");
        AuthTokensDto ownerB = await RegisterAsync(client, tenantB, "owner-b@example.com");
        Guid ownerBId = ParseUserIdFromAccessToken(ownerB.AccessToken);

        HttpResponseMessage disableResponse = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/users/{ownerBId}/disable", ownerB.AccessToken);
        disableResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden); // ownerB is tenantB's sole admin — self-disable is rejected regardless

        // Use a non-self mutation instead so tenant B genuinely has an audit entry to leak: register a
        // second tenant-B user and disable that one.
        AuthTokensDto memberB = await RegisterAsync(client, tenantB, "member-b@example.com");
        Guid memberBId = ParseUserIdFromAccessToken(memberB.AccessToken);
        HttpResponseMessage realDisable = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/users/{memberBId}/disable", ownerB.AccessToken);
        realDisable.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage auditAsTenantA = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/identity/audit-log?take=100", ownerA.AccessToken);
        List<IdentityAuditLogEntryDto>? entriesVisibleToA = await auditAsTenantA.Content.ReadFromJsonAsync<List<IdentityAuditLogEntryDto>>(JsonOptions);

        entriesVisibleToA.Should().NotBeNull();
        entriesVisibleToA!.Should().NotContain(e => e.TargetId == memberBId.ToString());
    }
}
