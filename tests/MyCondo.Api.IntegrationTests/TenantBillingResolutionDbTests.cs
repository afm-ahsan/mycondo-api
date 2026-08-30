using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Subscription.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof for the tenant-facing CondoBD SaaS billing-resolution read
/// (<c>GET /api/v1/subscription/billing-resolution</c>, ADR-032/ADR-034 Task 14I) against a real,
/// ephemeral PostgreSQL container — same real-HTTP/hand-crafted-tenant-session technique
/// <see cref="TenantLifecycleEnforcementDbTests"/> already establishes for the underlying lifecycle
/// behavior this endpoint deliberately opts into via <c>IBillingResolutionOperation</c>. Needs a Docker
/// daemon, same disclosed limitation as every other <see cref="PostgresApiFactory"/>-backed test.
/// </summary>
public class TenantBillingResolutionDbTests : IClassFixture<PostgresApiFactory>
{
    private const string BillingResolutionPath = "/api/v1/subscription/billing-resolution";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresApiFactory _factory;

    public TenantBillingResolutionDbTests(PostgresApiFactory factory)
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

    private async Task IssueOutstandingInvoiceAsync(Guid tenantId, DateOnly dueDate, decimal amount = 8000m, string currency = "BDT")
    {
        DateOnly periodStart = dueDate.AddDays(-30);
        DateTimeOffset issuedAt = new(periodStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionInvoiceRepository invoices = scope.ServiceProvider.GetRequiredService<ISubscriptionInvoiceRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        (SubscriptionInvoice invoice, IReadOnlyList<SubscriptionInvoiceLine> lines) = SubscriptionInvoice.Issue(
            tenantId, OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}", periodStart, dueDate, periodStart,
            dueDate, currency,
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, amount, 0m, amount,
                "Professional (Monthly)")],
            issuedAt);

        invoices.Add(invoice);
        scope.ServiceProvider.GetRequiredService<MyCondoDbContext>().Set<SubscriptionInvoiceLine>().AddRange(lines);
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

    private static async Task<HttpResponseMessage> GetBillingResolutionAsync(HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BillingResolutionPath);
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<string> CodeOf(HttpResponseMessage response) => await response.Content.ReadAsStringAsync();

    [Fact]
    public async Task Authorized_Tenant_Administrator_Can_Read_Own_Organizations_Billing_Resolution_Data()
    {
        string slug = "billres-admin";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com"); // first user = OrganizationAdmin

        HttpResponseMessage response = await GetBillingResolutionAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TenantBillingResolutionDto? dto = await response.Content.ReadFromJsonAsync<TenantBillingResolutionDto>(JsonOptions);
        dto.Should().NotBeNull();
        dto!.SubscriptionStatus.Should().Be("Active");
    }

    [Fact]
    public async Task Ordinary_User_Without_The_Administration_Permission_Receives_Forbidden()
    {
        string slug = "billres-ordinary";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient bootstrapClient = _factory.CreateClient();
        await RegisterAsync(bootstrapClient, tenantId, $"admin-{slug}@example.com"); // first user = OrganizationAdmin

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto ordinaryTokens = await RegisterAsync(client, tenantId, $"no-permissions-{slug}@example.com");

        HttpResponseMessage response = await GetBillingResolutionAsync(client, ordinaryTokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unauthenticated_Requests_Are_Rejected()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(BillingResolutionPath);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Tenant_A_Administrator_Cannot_See_Tenant_Bs_Outstanding_Invoices()
    {
        string slugA = "billres-tenanta";
        string slugB = "billres-tenantb";
        Guid tenantA = await SeedActiveTenantAsync(slugA);
        Guid tenantB = await SeedActiveTenantAsync(slugB);
        await SeedSubscriptionAsync(tenantA, OrganizationSubscriptionStatus.Active);
        await SeedSubscriptionAsync(tenantB, OrganizationSubscriptionStatus.Active);
        await IssueOutstandingInvoiceAsync(tenantA, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));

        using HttpClient clientA = _factory.CreateClient();
        AuthTokensDto tokensA = await RegisterAsync(clientA, tenantA, $"admin-{slugA}@example.com");
        using HttpClient clientB = _factory.CreateClient();
        AuthTokensDto tokensB = await RegisterAsync(clientB, tenantB, $"admin-{slugB}@example.com");

        TenantBillingResolutionDto? dtoA =
            await (await GetBillingResolutionAsync(clientA, tokensA.AccessToken)).Content.ReadFromJsonAsync<TenantBillingResolutionDto>(JsonOptions);
        TenantBillingResolutionDto? dtoB =
            await (await GetBillingResolutionAsync(clientB, tokensB.AccessToken)).Content.ReadFromJsonAsync<TenantBillingResolutionDto>(JsonOptions);

        dtoA!.OutstandingInvoices.Should().ContainSingle();
        dtoB!.OutstandingInvoices.Should().BeEmpty();
    }

    [Theory]
    [InlineData(OrganizationSubscriptionStatus.Restricted)]
    [InlineData(OrganizationSubscriptionStatus.Expired)]
    [InlineData(OrganizationSubscriptionStatus.Canceled)]
    public async Task ReadOnly_Subscription_States_Still_Reach_The_Billing_Resolution_Operation(
        OrganizationSubscriptionStatus status)
    {
        string slug = $"billres-{status.ToString().ToLowerInvariant()}";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, status);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetBillingResolutionAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TenantBillingResolutionDto? dto = await response.Content.ReadFromJsonAsync<TenantBillingResolutionDto>(JsonOptions);
        dto!.SubscriptionStatus.Should().Be(status.ToString());
    }

    [Fact]
    public async Task Suspended_Organization_Still_Blocks_Billing_Resolution_Like_Every_Other_Request()
    {
        string slug = "billres-suspended";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");
        await SuspendTenantAsync(tenantId);

        HttpResponseMessage response = await GetBillingResolutionAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeOf(response)).Should().Contain("tenant_suspended");
    }

    [Fact]
    public async Task Closed_Organization_Still_Blocks_Billing_Resolution_Like_Every_Other_Request()
    {
        string slug = "billres-closed";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");
        await CloseTenantAsync(tenantId);

        HttpResponseMessage response = await GetBillingResolutionAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeOf(response)).Should().Contain("tenant_closed");
    }

    [Fact]
    public async Task Outstanding_Invoice_Reports_Backend_Derived_DaysOverdue_And_Excludes_Platform_Only_Fields()
    {
        string slug = "billres-overdue";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);
        await IssueOutstandingInvoiceAsync(tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetBillingResolutionAsync(client, tokens.AccessToken);
        string rawBody = await response.Content.ReadAsStringAsync();

        TenantBillingResolutionDto? dto = JsonSerializer.Deserialize<TenantBillingResolutionDto>(rawBody, JsonOptions);
        dto!.OutstandingInvoices.Should().ContainSingle();
        dto.OutstandingInvoices[0].DaysOverdue.Should().BeGreaterThan(0);
        dto.OutstandingInvoices[0].Status.Should().Be(nameof(SubscriptionInvoiceStatus.Issued));

        // Never exposes platform-only identifiers/metadata (invoice id, tenant id, organization name).
        string lowerBody = rawBody.ToLowerInvariant();
        lowerBody.Should().NotContain("invoiceid");
        lowerBody.Should().NotContain("organizationname");
    }

    [Fact]
    public async Task Outstanding_Summary_Keeps_Different_Currencies_Separate()
    {
        string slug = "billres-multicurrency";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);
        await IssueOutstandingInvoiceAsync(tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), 5000m, "BDT");
        await IssueOutstandingInvoiceAsync(tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), 100m, "USD");

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetBillingResolutionAsync(client, tokens.AccessToken);
        TenantBillingResolutionDto? dto = await response.Content.ReadFromJsonAsync<TenantBillingResolutionDto>(JsonOptions);

        dto!.OutstandingSummary.Should().HaveCount(2);
        dto.OutstandingSummary.Should().Contain(x => x.Currency == "BDT" && x.OutstandingAmount == 5000m);
        dto.OutstandingSummary.Should().Contain(x => x.Currency == "USD" && x.OutstandingAmount == 100m);
    }

    [Fact]
    public async Task Platform_Billing_Endpoints_Remain_Unreachable_And_The_Platform_Permission_Is_Not_Granted_Tenant_Side()
    {
        string slug = "billres-noplatform";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedSubscriptionAsync(tenantId, OrganizationSubscriptionStatus.Active);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/platform/billing/outstanding");
        request.Headers.Authorization = new("Bearer", tokens.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }
}
