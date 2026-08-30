using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof that <c>TenantLifecycleBehavior</c> (ADR-032 §6, Task 10) is actually reached via real
/// HTTP requests and composes correctly with the existing RBAC and ADR-033 feature-entitlement gates.
/// <c>TenantLifecycleBehaviorTests</c>/<c>LifecyclePolicyTests</c> already cover the decision logic in
/// isolation with fakes — this class instead proves the real wiring: DI registration order/placement
/// relative to <c>FeatureEntitlementBehavior</c>, exception→403 mapping with distinguishing error codes,
/// and that a session token issued while the tenant was Active is re-evaluated on every subsequent request
/// (not just at login/register) once the organization/subscription lifecycle changes underneath it.
///
/// Needs a Docker daemon, same disclosed limitation as every other PostgresApiFactory-backed test.
/// </summary>
public class TenantLifecycleEnforcementDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresApiFactory _factory;

    public TenantLifecycleEnforcementDbTests(PostgresApiFactory factory)
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

    private async Task SetTenantStatusAsync(Guid tenantId, Action<Tenant, DateTimeOffset> apply)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Tenant tenant = (await tenants.GetByIdAsync(tenantId, CancellationToken.None))!;
        apply(tenant, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private Task SuspendTenantAsync(Guid tenantId) => SetTenantStatusAsync(tenantId, (t, now) => t.Suspend(now));

    private Task CloseTenantAsync(Guid tenantId) => SetTenantStatusAsync(tenantId, (t, now) => t.Close(now));

    private async Task SeedSubscriptionAsync(Guid tenantId, OrganizationSubscriptionStatus status)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IOrganizationSubscriptionRepository subscriptions = scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // No package version needs to exist for these lifecycle tests — the read/write access-mode
        // decision (TenantAccessMode) never inspects package/feature composition (that is
        // ITenantEntitlementService's separate, deliberately-unrelated concern, ADR-033 §14).
        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenantId, SubscriptionPackageVersionId.New(), BillingCycle.Monthly, StartDate, null, null,
            1000m, 0m, "BDT", ActivatedAt, autoRenew: false);

        switch (status)
        {
            case OrganizationSubscriptionStatus.Active:
                break;
            case OrganizationSubscriptionStatus.PastDue:
                subscription.MarkPastDue();
                break;
            case OrganizationSubscriptionStatus.Restricted:
                subscription.MarkPastDue();
                subscription.Restrict(ActivatedAt);
                break;
            case OrganizationSubscriptionStatus.Expired:
                subscription.MarkPastDue();
                subscription.Restrict(ActivatedAt);
                subscription.Expire(ActivatedAt);
                break;
            case OrganizationSubscriptionStatus.Canceled:
                subscription.Cancel(ActivatedAt);
                break;
        }

        subscriptions.Add(subscription);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
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

    private static async Task<HttpResponseMessage> GetResidentsAsync(HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/residents?page=1&pageSize=20");
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> CreateResidentAsync(HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/residents")
        {
            Content = JsonContent.Create(new
            {
                flatId = Guid.NewGuid(),
                fullName = "New Resident",
                phone = (string?)null,
                email = (string?)null,
                residentType = "Owner",
            })
        };
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> GetInvoicesAsync(HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/invoices?page=1&pageSize=20");
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> RecordPaymentAsync(HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/payments")
        {
            Content = JsonContent.Create(new
            {
                flatId = Guid.NewGuid(),
                amount = 100m,
                paymentMethod = "Cash",
                referenceNumber = (string?)null,
                businessDate = StartDate,
                description = (string?)null,
            })
        };
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> CorrectReadingAsync(HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/readings/{Guid.NewGuid()}/correct")
        {
            Content = JsonContent.Create(new
            {
                previousReading = 10m,
                presentReading = 20m,
                readingDate = StartDate,
                overrideReason = (string?)null,
                reason = "Meter misread",
            })
        };
        request.Headers.Authorization = new("Bearer", accessToken);
        request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        return await client.SendAsync(request);
    }

    private static async Task<string> CodeOf(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        return body;
    }

    [Fact]
    public async Task Active_Organization_Active_Subscription_Read_And_Write_Both_Reach_The_Handler()
    {
        string slug = "lifecycle-active";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        (await GetResidentsAsync(client, tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        // 404 (unknown FlatId), not 403 — proves the write reached the handler, i.e. lifecycle did not block it.
        (await CreateResidentAsync(client, tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task No_Subscription_Ever_Provisioned_Still_Allows_Full_Access()
    {
        // ADR-032 Task 10 §21/§77 — no subscription-aware provisioning path exists yet; this must not be
        // treated as a lockout.
        string slug = "lifecycle-nosub";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        (await GetResidentsAsync(client, tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await CreateResidentAsync(client, tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PastDue_Subscription_Allows_Ordinary_Write()
    {
        string slug = "lifecycle-pastdue";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.PastDue);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        (await CreateResidentAsync(client, tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(OrganizationSubscriptionStatus.Restricted, "subscription_restricted")]
    [InlineData(OrganizationSubscriptionStatus.Expired, "subscription_expired")]
    [InlineData(OrganizationSubscriptionStatus.Canceled, "subscription_expired")]
    public async Task ReadOnly_Subscription_States_Allow_Reads_And_Deny_Writes_With_The_Distinguishing_Code(
        OrganizationSubscriptionStatus status, string expectedCode)
    {
        string slug = $"lifecycle-{status.ToString().ToLowerInvariant()}";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, status);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        (await GetResidentsAsync(client, tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage writeResponse = await CreateResidentAsync(client, tokens.AccessToken);
        writeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeOf(writeResponse)).Should().Contain(expectedCode);
    }

    [Fact]
    public async Task Suspended_Organization_Denies_Access_On_An_Already_Issued_Token_Even_With_An_Active_Subscription()
    {
        // The critical integration point (ADR-032 Task 10 §43): login already blocks a Suspended tenant,
        // but a token minted *before* suspension must also stop working on the very next request — not
        // only at the next login/refresh.
        string slug = "lifecycle-suspended";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        await SuspendTenantAsync(tenantId);

        HttpResponseMessage response = await GetResidentsAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeOf(response)).Should().Contain("tenant_suspended");
    }

    [Fact]
    public async Task Closed_Organization_Denies_Access_On_An_Already_Issued_Token()
    {
        string slug = "lifecycle-closed";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        await CloseTenantAsync(tenantId);

        HttpResponseMessage response = await GetResidentsAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeOf(response)).Should().Contain("tenant_closed");
    }

    [Fact]
    public async Task Suspended_Organization_Dominates_Even_With_A_Fully_Active_Subscription_And_Permission()
    {
        // Task 10 §69 — organization dominance over the subscription axis.
        string slug = "lifecycle-org-dominance";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");
        await SuspendTenantAsync(tenantId);

        HttpResponseMessage response = await CreateResidentAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeOf(response)).Should().Contain("tenant_suspended");
    }

    [Fact]
    public async Task Restricted_Subscription_Blocks_A_ResourceDerived_FeatureGated_Write_Before_Entitlement_Resolution()
    {
        // Task 10 §35/§71 — lifecycle enforcement must compose with Task 09A's resource-derived
        // resolution, and must run BEFORE it: the ReadingId below does not exist, so if the lifecycle
        // check did not short-circuit first, this would fail differently (404/resolver error), not with
        // the lifecycle code.
        string slug = "lifecycle-resource-derived";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Restricted);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await CorrectReadingAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeOf(response)).Should().Contain("subscription_restricted");
    }

    [Fact]
    public async Task Restricted_Subscription_Allows_A_Finance_Read_And_Denies_A_Finance_Write()
    {
        // Task 10 §39 — Finance reads stay available; ordinary Finance mutations are blocked (never
        // conflated with CondoBD platform-billing resolution, §40).
        string slug = "lifecycle-finance";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Restricted);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        (await GetInvoicesAsync(client, tokens.AccessToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage paymentResponse = await RecordPaymentAsync(client, tokens.AccessToken);
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeOf(paymentResponse)).Should().Contain("subscription_restricted");
    }

    [Fact]
    public async Task Active_Lifecycle_Still_Enforces_Ordinary_RBAC_Denials_Unmasked()
    {
        // Task 10 §33/§68 — lifecycle must not swallow or mask an ordinary RBAC denial when lifecycle
        // itself permits access. The second-registered user holds no permissions.
        string slug = "lifecycle-rbac";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient bootstrapClient = _factory.CreateClient();
        await RegisterAsync(bootstrapClient, tenantId, $"admin-{slug}@example.com"); // first user = OrganizationAdmin

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto secondUserTokens = await RegisterAsync(client, tenantId, $"no-permissions-{slug}@example.com");

        HttpResponseMessage response = await GetResidentsAsync(client, secondUserTokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await CodeOf(response);
        body.Should().NotContain("tenant_suspended");
        body.Should().NotContain("subscription_restricted");
    }

    // ADR-032 Task 11 §5/§7/§34 — the session DTO carries the same lifecycle outcome this class already
    // proves is enforced per-request, so the frontend never has to re-derive the policy independently.

    [Fact]
    public async Task Active_Session_Reports_Full_Lifecycle_Access_Mode_With_No_Reason()
    {
        string slug = "lifecycle-session-full";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        tokens.User.LifecycleAccessMode.Should().Be("Full");
        tokens.User.LifecycleReason.Should().BeNull();
    }

    [Theory]
    [InlineData(OrganizationSubscriptionStatus.Restricted, "subscription_restricted")]
    [InlineData(OrganizationSubscriptionStatus.Expired, "subscription_expired")]
    [InlineData(OrganizationSubscriptionStatus.Canceled, "subscription_expired")]
    public async Task ReadOnly_Subscription_States_Report_ReadOnly_Lifecycle_Access_Mode_With_The_Distinguishing_Reason(
        OrganizationSubscriptionStatus status, string expectedReason)
    {
        string slug = $"lifecycle-session-{status.ToString().ToLowerInvariant()}";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, status);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        tokens.User.LifecycleAccessMode.Should().Be("ReadOnly");
        tokens.User.LifecycleReason.Should().Be(expectedReason);
    }

    [Fact]
    public async Task Refresh_Reports_Denied_Lifecycle_Access_Mode_After_The_Organization_Is_Suspended_Mid_Session()
    {
        // RotateAsync itself never checks tenant status (that remains TenantLifecycleBehavior's
        // per-request job — see §33/§34 above), but ResolveAsync recomputes the lifecycle fields fresh
        // on every refresh, so the session *snapshot* the frontend receives is never stale even though
        // the refresh call itself is not blocked outright.
        string slug = "lifecycle-session-suspended-midsession";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto initial = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");
        initial.User.LifecycleAccessMode.Should().Be("Full");

        await SuspendTenantAsync(tenantId);

        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { tenantId });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);

        refreshed.Should().NotBeNull();
        refreshed!.User.LifecycleAccessMode.Should().Be("Denied");
        refreshed.User.LifecycleReason.Should().Be("tenant_suspended");
    }

    [Fact]
    public async Task Refresh_Reports_Denied_Lifecycle_Access_Mode_After_The_Organization_Is_Closed_Mid_Session()
    {
        string slug = "lifecycle-session-closed-midsession";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto initial = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");
        initial.User.LifecycleAccessMode.Should().Be("Full");

        await CloseTenantAsync(tenantId);

        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { tenantId });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);

        refreshed.Should().NotBeNull();
        refreshed!.User.LifecycleAccessMode.Should().Be("Denied");
        refreshed.User.LifecycleReason.Should().Be("tenant_closed");
    }
}
