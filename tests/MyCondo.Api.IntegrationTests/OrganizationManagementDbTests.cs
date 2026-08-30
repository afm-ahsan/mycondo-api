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
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
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
public class OrganizationManagementDbTests : IClassFixture<PostgresApiFactory>, IAsyncLifetime
{
    private const string Issuer = "https://api.condobd.com";
    private const string PlatformAudience = "https://platform.condobd.com";
    private const string SigningKey = "test-only-signing-key-not-for-any-real-environment";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;
    private Guid _subscriptionPackageVersionId;

    public OrganizationManagementDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    // Task 12A (ADR-033) requires provisioning to reference a real, explicitly active/current/effective
    // SubscriptionPackageVersion — there is no package-administration endpoint yet (out of this task's
    // scope), so this seeds one directly through the ambient repositories/domain factory, the same
    // pattern GateFeatureEntitlementDbTests.SeedPackageVersionAsync already uses. Re-seeded per test
    // method (xUnit constructs a fresh class instance per [Fact]) with a unique Code each time — cheap,
    // and avoids any cross-test coupling through shared package state. This package intentionally has
    // zero SubscriptionPackageFeature rows, so a tenant provisioned against it resolves to "not entitled"
    // for every feature — the opposite of the legacy grandfathered package's full-catalogue grant,
    // proving a newly provisioned tenant no longer depends on "no subscription -> Full".
    public async Task InitializeAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create(
            $"IT-{Guid.NewGuid():N}"[..20], "Integration Test Package", description: null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id,
            version: 1,
            effectiveFrom: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            effectiveUntil: null,
            monthlyPrice: 1000m,
            quarterlyPrice: null,
            semiAnnualPrice: null,
            annualPrice: null,
            currency: "BDT");
        version.Activate();
        package.Activate();
        package.SetCurrentVersion(version.Id);

        packages.Add(package);
        versions.Add(version);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        _subscriptionPackageVersionId = version.Id.Value;
    }

    public Task DisposeAsync() => Task.CompletedTask;

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

    private object NewOrganizationBody(string code, string slug, string adminEmail) => new
    {
        name = "Integration Test Org",
        code,
        slug,
        administratorFullName = "Test Admin",
        administratorEmail = adminEmail,
        administratorPassword = "Correct-Horse-Battery-9",
        enabledModuleKeys = new[] { "billing", "payments" },
        subscriptionPackageVersionId = _subscriptionPackageVersionId,
        billingCycle = "Monthly",
        autoRenew = true,
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

    [Fact]
    public async Task Provisioning_Creates_Exactly_One_Current_OrganizationSubscription_Referencing_The_Selected_Package_Version()
    {
        using HttpClient client = CreatePlatformClient("platform.organization.create");

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E7", "e2e-org-7", "admin@e2e-org-7.test"));
        ProvisionOrganizationResult result =
            (await createResponse.Content.ReadFromJsonAsync<ProvisionOrganizationResult>(JsonOptions))!;

        using IServiceScope scope = _factory.Services.CreateScope();
        IOrganizationSubscriptionRepository subscriptions =
            scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        OrganizationSubscription? subscription =
            await subscriptions.GetCurrentForTenantAsync(result.TenantId, CancellationToken.None);

        subscription.Should().NotBeNull();
        subscription!.PackageVersionId.Value.Should().Be(_subscriptionPackageVersionId);
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Active);
    }

    [Fact]
    public async Task Provisioning_Rejects_An_Unknown_SubscriptionPackageVersionId()
    {
        using HttpClient client = CreatePlatformClient("platform.organization.create");

        object body = new
        {
            name = "Integration Test Org",
            code = "E2E8",
            slug = "e2e-org-8",
            administratorFullName = "Test Admin",
            administratorEmail = "admin@e2e-org-8.test",
            administratorPassword = "Correct-Horse-Battery-9",
            enabledModuleKeys = new[] { "billing", "payments" },
            subscriptionPackageVersionId = Guid.NewGuid(),
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/platform/organizations", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Newly_Provisioned_Tenant_Is_Not_Entitled_To_A_Feature_Its_Package_Does_Not_Grant()
    {
        // The seeded test package (InitializeAsync) has zero SubscriptionPackageFeature rows — if
        // provisioning still fell back to the legacy "no subscription -> Full" compatibility rule, this
        // request would incorrectly succeed. It must instead be blocked by the entitlement resolver
        // (403 feature_not_entitled), proving the new tenant resolves entitlements through its explicit
        // OrganizationSubscription rather than the legacy grandfathering path.
        using HttpClient platformClient = CreatePlatformClient("platform.organization.create");
        HttpResponseMessage createResponse = await platformClient.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E9", "e2e-org-9", "admin@e2e-org-9.test"));
        ProvisionOrganizationResult result =
            (await createResponse.Content.ReadFromJsonAsync<ProvisionOrganizationResult>(JsonOptions))!;

        using HttpClient tenantClient = _factory.CreateClient();
        HttpResponseMessage loginResponse = await tenantClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            tenantId = result.TenantId,
            email = "admin@e2e-org-9.test",
            password = "Correct-Horse-Battery-9",
        });
        JsonDocument loginBody = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        string accessToken = loginBody.RootElement.GetProperty("accessToken").GetString()!;
        tenantClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage gatesResponse = await tenantClient.GetAsync("/api/v1/properties/gates");

        gatesResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        JsonDocument problem = JsonDocument.Parse(await gatesResponse.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("feature_not_entitled");
    }

    [Fact]
    public async Task Get_Subscription_Returns_Exact_Package_Version_Lifecycle_And_Commercial_Snapshot()
    {
        using HttpClient client = CreatePlatformClient("platform.organization.create", "platform.subscription.read");

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E10", "e2e-org-10", "admin@e2e-org-10.test"));
        ProvisionOrganizationResult result =
            (await createResponse.Content.ReadFromJsonAsync<ProvisionOrganizationResult>(JsonOptions))!;

        HttpResponseMessage subscriptionResponse =
            await client.GetAsync($"/api/v1/platform/organizations/{result.TenantId}/subscription");

        subscriptionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        OrganizationSubscriptionDto dto =
            (await subscriptionResponse.Content.ReadFromJsonAsync<OrganizationSubscriptionDto>(JsonOptions))!;

        dto.TenantId.Should().Be(result.TenantId);
        dto.HasSubscription.Should().BeTrue();
        dto.Subscription.Should().NotBeNull();
        dto.Subscription!.PackageVersionId.Should().Be(_subscriptionPackageVersionId);
        dto.Subscription.PackageVersion.Should().Be(1);
        dto.Subscription.Status.Should().Be(nameof(OrganizationSubscriptionStatus.Active));
        dto.Subscription.BillingCycle.Should().Be(nameof(BillingCycle.Monthly));
        dto.Subscription.Currency.Should().Be("BDT");
        dto.Subscription.BasePrice.Should().Be(1000m);
        dto.Subscription.Discount.Should().Be(0m);
        dto.Subscription.EffectivePrice.Should().Be(1000m);
        dto.Subscription.AutoRenew.Should().BeTrue();

        // The seeded test package (InitializeAsync) grants zero features — every non-core feature must
        // therefore resolve to DefaultDisabled, and every core feature to Core, proving this endpoint
        // reuses ITenantEntitlementService's precedence rather than a second entitlement engine.
        dto.Features.Should().NotBeEmpty();
        dto.Features.Should().Contain(f => f.Source == "Core" && f.Enabled);
        dto.Features.Where(f => f.Source != "Core").Should().OnlyContain(f => f.Source == "DefaultDisabled" && !f.Enabled);
    }

    [Fact]
    public async Task Get_Subscription_Requires_Its_Own_Permission_Not_Organization_Read()
    {
        using HttpClient createClient = CreatePlatformClient("platform.organization.create");
        HttpResponseMessage createResponse = await createClient.PostAsJsonAsync(
            "/api/v1/platform/organizations", NewOrganizationBody("E2E11", "e2e-org-11", "admin@e2e-org-11.test"));
        ProvisionOrganizationResult result =
            (await createResponse.Content.ReadFromJsonAsync<ProvisionOrganizationResult>(JsonOptions))!;

        using HttpClient organizationReadOnlyClient = CreatePlatformClient("platform.organization.read");
        HttpResponseMessage forbiddenResponse =
            await organizationReadOnlyClient.GetAsync($"/api/v1/platform/organizations/{result.TenantId}/subscription");

        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
