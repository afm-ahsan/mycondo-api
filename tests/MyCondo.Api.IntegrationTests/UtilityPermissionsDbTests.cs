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
/// Round-trip proof that Slice F's 8 new permissions (utility.meter.view/manage,
/// utility.rateplan.view/manage, utility.reading.view/record/finalize/correct) are actually
/// enforced by <c>PermissionEndpointFilter</c> — same pattern as <see cref="BillingPermissionsDbTests"/>
/// (one representative endpoint per permission, second-registered-user-holds-no-permissions).
///
/// Needs a Docker daemon, same disclosed limitation as every other PostgresApiFactory-backed test —
/// written and reviewed for correctness but not executed in the environment they were authored in.
/// </summary>
public class UtilityPermissionsDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public UtilityPermissionsDbTests(PostgresApiFactory factory)
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
    public async Task Get_Meters_Without_UtilityMeterView_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("utility-meter-view");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Get, $"/api/v1/meters?buildingId={Guid.NewGuid()}&page=1&pageSize=20", accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Install_Meter_Without_UtilityMeterManage_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("utility-meter-manage");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/meters", accessToken,
            new { buildingId = Guid.NewGuid(), utilityType = "Electricity", meterNumber = "MTR-001" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_RatePlans_Without_UtilityRatePlanView_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("utility-rateplan-view");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Get, $"/api/v1/rate-plans?buildingId={Guid.NewGuid()}&page=1&pageSize=20", accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_RatePlan_Without_UtilityRatePlanManage_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("utility-rateplan-manage");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/rate-plans", accessToken,
            new
            {
                buildingId = Guid.NewGuid(),
                utilityType = "Gas",
                name = "Flat Gas Charge",
                structure = "Fixed",
                fixedAmount = 800m,
                fixedServiceCharge = 0m,
                taxPercentage = 0m,
                effectiveFrom = "2026-01-01",
                slabs = Array.Empty<object>(),
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Readings_Without_UtilityReadingView_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("utility-reading-view");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Get, "/api/v1/readings?page=1&pageSize=20", accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Record_Reading_Without_UtilityReadingRecord_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("utility-reading-record");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, "/api/v1/readings", accessToken,
            new
            {
                meterId = Guid.NewGuid(),
                periodStart = "2026-03-01",
                periodEnd = "2026-03-31",
                previousReading = 0m,
                presentReading = 50m,
                readingDate = "2026-03-31",
                overrideReason = (string?)null,
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Finalize_Reading_Without_UtilityReadingFinalize_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("utility-reading-finalize");
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/readings/{Guid.NewGuid()}/finalize", accessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Correct_Reading_Without_UtilityReadingCorrect_Returns_403()
    {
        string accessToken = await RegisterUnauthorizedUserAsync("utility-reading-correct");
        using HttpClient client = _factory.CreateClient();

        // No X-Idempotency-Key header — the permission filter must reject before the idempotency
        // filter (registered second) ever runs, so this 403s rather than 400s.
        HttpResponseMessage response = await SendAuthedAsync(
            client, HttpMethod.Post, $"/api/v1/readings/{Guid.NewGuid()}/correct", accessToken,
            new { previousReading = 0m, presentReading = 55m, readingDate = "2026-03-31", overrideReason = (string?)null, reason = "Correction" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
