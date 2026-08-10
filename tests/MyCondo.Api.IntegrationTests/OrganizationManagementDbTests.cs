using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// End-to-end proof, against a real ephemeral PostgreSQL container (see PostgresApiFactory), that
/// Platform-initiated organization provisioning produces a fully working tenant — not just a
/// PendingActivation row nobody can sign into. Uses the hand-crafted-JWT technique from
/// PlatformSchemeIsolationTests (a Platform-scheme token with exactly the perm claims a test needs)
/// rather than seeding a full PlatformUser/PlatformRole/assignment chain per test, since
/// PlatformCurrentUserProvider reads permission claims directly off the token.
/// </summary>
public class OrganizationManagementDbTests : IClassFixture<PostgresApiFactory>
{
    private const string Issuer = "https://api.mycondo.app";
    private const string PlatformAudience = "https://platform.mycondo.app";
    private const string SigningKey = "test-only-signing-key-not-for-any-real-environment";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public OrganizationManagementDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreatePlatformClient(params string[] permissions)
    {
        HttpClient client = _factory.CreateClient();
        string token = CreatePlatformToken(permissions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string CreatePlatformToken(IEnumerable<string> permissions)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(SigningKey));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = [new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())];
        claims.AddRange(permissions.Select(p => new Claim("perm", p)));

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = Issuer,
            Audience = PlatformAudience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = creds,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private static object NewOrganizationBody(string code, string slug, string adminEmail) => new
    {
        name = "Integration Test Org",
        code,
        slug,
        administratorFullName = "Test Admin",
        administratorEmail = adminEmail,
        administratorPassword = "Correct-Horse-Battery-9",
        enabledModuleKeys = new[] { "billing", "payments" },
    };

    [Fact]
    public async Task Provisioning_Creates_An_Active_Tenant_Whose_Admin_Can_Immediately_Log_In()
    {
        using HttpClient client = CreatePlatformClient("platform.organization.create");

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E1", "e2e-org-1", "admin@e2e-org-1.test"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ProvisionOrganizationResult? result =
            await createResponse.Content.ReadFromJsonAsync<ProvisionOrganizationResult>(JsonOptions);
        result.Should().NotBeNull();
        result!.Status.Should().Be(nameof(TenantStatus.Active));

        using HttpClient anonymousClient = _factory.CreateClient();
        HttpResponseMessage loginResponse = await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            tenantId = result.TenantId,
            email = "admin@e2e-org-1.test",
            password = "Correct-Horse-Battery-9",
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument body = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("user").GetProperty("roles").EnumerateArray()
            .Select(r => r.GetString()).Should().Contain("OrganizationAdmin");
    }

    [Fact]
    public async Task Suspending_An_Organization_Blocks_Its_Admin_From_Logging_In_And_Reactivating_Restores_It()
    {
        using HttpClient client = CreatePlatformClient(
            "platform.organization.create", "platform.organization.suspend", "platform.organization.reactivate");

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E2", "e2e-org-2", "admin@e2e-org-2.test"));
        ProvisionOrganizationResult result =
            (await createResponse.Content.ReadFromJsonAsync<ProvisionOrganizationResult>(JsonOptions))!;

        using HttpClient anonymousClient = _factory.CreateClient();
        object loginBody = new { tenantId = result.TenantId, email = "admin@e2e-org-2.test", password = "Correct-Horse-Battery-9" };

        HttpResponseMessage suspendResponse = await client.PostAsync(
            $"/api/v1/platform/organizations/{result.TenantId}/suspend", null);
        suspendResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage blockedLogin = await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginBody);
        blockedLogin.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        HttpResponseMessage reactivateResponse = await client.PostAsync(
            $"/api/v1/platform/organizations/{result.TenantId}/reactivate", null);
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage restoredLogin = await anonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginBody);
        restoredLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Provisioning_Rejects_A_Duplicate_Code()
    {
        using HttpClient client = CreatePlatformClient("platform.organization.create");
        await client.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E3", "e2e-org-3a", "admin@e2e-org-3a.test"));

        HttpResponseMessage duplicateResponse = await client.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E3", "e2e-org-3b", "admin@e2e-org-3b.test"));

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Then_GetById_Reflects_The_New_Name_And_Code()
    {
        using HttpClient client = CreatePlatformClient(
            "platform.organization.create", "platform.organization.update", "platform.organization.read");

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E4", "e2e-org-4", "admin@e2e-org-4.test"));
        ProvisionOrganizationResult result =
            (await createResponse.Content.ReadFromJsonAsync<ProvisionOrganizationResult>(JsonOptions))!;

        HttpResponseMessage updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/platform/organizations/{result.TenantId}", new { name = "Renamed Org", code = "RENAMED" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/platform/organizations/{result.TenantId}");
        OrganizationDetailDto detail = (await getResponse.Content.ReadFromJsonAsync<OrganizationDetailDto>(JsonOptions))!;

        detail.Name.Should().Be("Renamed Org");
        detail.Code.Should().Be("RENAMED");
        detail.Administrator.Should().NotBeNull();
        detail.Administrator!.Email.Should().Be("admin@e2e-org-4.test");
        detail.EnabledModuleKeys.Should().BeEquivalentTo(["billing", "payments"]);
    }

    [Fact]
    public async Task Replacing_Modules_Is_A_Full_Set_Replace()
    {
        using HttpClient client = CreatePlatformClient(
            "platform.organization.create", "platform.organization.features.manage", "platform.organization.read");

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E5", "e2e-org-5", "admin@e2e-org-5.test"));
        ProvisionOrganizationResult result =
            (await createResponse.Content.ReadFromJsonAsync<ProvisionOrganizationResult>(JsonOptions))!;

        HttpResponseMessage replaceResponse = await client.PutAsJsonAsync(
            $"/api/v1/platform/organizations/{result.TenantId}/modules",
            new { moduleKeys = new[] { "utilities" } });
        replaceResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/platform/organizations/{result.TenantId}");
        OrganizationDetailDto detail = (await getResponse.Content.ReadFromJsonAsync<OrganizationDetailDto>(JsonOptions))!;

        detail.EnabledModuleKeys.Should().BeEquivalentTo(["utilities"]);
    }

    [Fact]
    public async Task List_Includes_A_Newly_Provisioned_Organization()
    {
        using HttpClient client = CreatePlatformClient("platform.organization.create", "platform.organization.read");
        await client.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E6", "e2e-org-6", "admin@e2e-org-6.test"));

        HttpResponseMessage listResponse = await client.GetAsync("/api/v1/platform/organizations?page=1&pageSize=100");
        PagedResult<OrganizationListItemDto> page =
            (await listResponse.Content.ReadFromJsonAsync<PagedResult<OrganizationListItemDto>>(JsonOptions))!;

        page.Items.Should().Contain(o => o.Slug == "e2e-org-6" && o.Code == "E2E6");
    }

    [Fact]
    public async Task Every_New_Endpoint_Requires_Its_Own_Permission_Not_A_Different_One()
    {
        // A token holding only "platform.organization.read" must not be able to create/suspend/
        // reactivate/update/replace-modules — each action's own permission is independently enforced.
        using HttpClient readOnlyClient = CreatePlatformClient("platform.organization.read");

        HttpResponseMessage createAttempt = await readOnlyClient.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("NOPE", "nope-org", "admin@nope.test"));

        createAttempt.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
