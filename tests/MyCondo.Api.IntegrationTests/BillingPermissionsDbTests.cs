using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof that Slice E's 5 new permissions (billing.rule.view, billing.rule.manage,
/// billing.invoice.view, billing.invoice.generate, billing.invoice.void) are actually enforced by
/// <c>PermissionEndpointFilter</c> on the endpoints that declare them — not just present in the
/// seeded catalogue. Mirrors <see cref="RoleEndpointsDbTests"/>'s
/// <c>Second_User_Of_Tenant_Is_Not_Bootstrapped_And_Cannot_View_Roles</c> pattern: only a tenant's
/// first registered user is bootstrapped as SuperAdmin (see <c>ISuperAdminBootstrapper</c>), so a
/// second registered user in the same tenant holds no permissions at all and is the standard way to
/// prove a permission-gated endpoint actually rejects an unauthorized caller.
///
/// One representative endpoint per permission, not all 9 Slice E endpoints — <c>PermissionEndpointFilter</c>
/// applies the identical check regardless of which endpoint declares it (verified once per prior
/// slice's convention, e.g. <see cref="RoleEndpointsDbTests"/> tests one endpoint for role.manage, not
/// every roles endpoint). The filter runs before the Mediator pipeline (and thus before
/// FluentValidation), so the request bodies below don't need to be otherwise valid — a 403 must come
/// back before the handler, or validator, ever runs.
///
/// Needs a Docker daemon, same disclosed limitation as every other PostgresApiFactory-backed test —
/// written and reviewed for correctness but not executed in the environment they were authored in.
/// </summary>
public class BillingPermissionsDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public BillingPermissionsDbTests(PostgresApiFactory factory)
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

    /// <summary>Registers a tenant's first (SuperAdmin-bootstrapped, all-permissions) user and a
    /// second (no-permissions) user in one call — the second user's token is what every test below
    /// sends.</summary>
    private async Task<string> RegisterUnauthorizedUserAsync(string tenantSlug)
    {
        Guid tenantId = await SeedActiveTenantAsync(tenantSlug);
        using HttpClient bootstrapClient = _factory.CreateClient();
        await RegisterAsync(bootstrapClient, tenantId, $"owner-{tenantSlug}@example.com");

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto secondUserTokens = await RegisterAsync(client, tenantId, $"no-permissions-{tenantSlug}@example.com");
        return secondUserTokens.AccessToken;
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
    public async Task Get_ServiceChargeRules_Without_BillingRuleView_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("billing-rule-view");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Get, $"/api/v1/service-charge-rules?buildingId={Guid.NewGuid()}&page=1&pageSize=20", accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ServiceChargeRule_Without_BillingRuleManage_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("billing-rule-manage");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/service-charge-rules", accessToken,
            new
            {
                buildingId = Guid.NewGuid(),
                category = "ServiceCharge",
                name = "Standard Charge",
                calculationMethod = "FixedAmount",
                rate = 1500m,
                unitTypeFilter = (string?)null,
                frequency = "Monthly",
                effectiveFrom = "2026-01-01",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Invoices_Without_BillingInvoiceView_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("billing-invoice-view");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/invoices?page=1&pageSize=20", accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateInvoiceBatch_Without_BillingInvoiceGenerate_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("billing-invoice-generate");
        using HttpClient client = _factory.CreateClient();

        // No X-Idempotency-Key header — the permission filter must reject before the idempotency
        // filter (registered second) ever runs, so this 403s rather than 400s.
        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/invoices/generate-batch", accessToken,
            new { buildingId = Guid.NewGuid(), periodStart = "2026-03-01", periodEnd = "2026-03-31" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VoidInvoice_Without_BillingInvoiceVoid_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("billing-invoice-void");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/invoices/{Guid.NewGuid()}/void", accessToken,
            new { reason = "Issued in error" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
