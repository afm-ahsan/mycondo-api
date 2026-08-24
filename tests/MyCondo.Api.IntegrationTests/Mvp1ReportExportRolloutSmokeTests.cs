using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Full-stack, real-Postgres + real-headless-Chromium proof that a representative cross-section of
/// the MVP-1 report-export rollout's 33 new export endpoints (added on top of the Trial Balance pilot
/// covered by <see cref="TrialBalanceExportEndpointDbTests"/>) actually produce well-formed CSV/PDF
/// bytes over HTTP through the real <c>IReportExportService</c> pipeline — not just a mocked-handler
/// unit test. Deliberately spans distinct document shapes and feature areas rather than every report,
/// to keep seeding simple (no cross-entity setup needed — every report picked here is a valid,
/// already-covered empty/zero-param state): a Totals-bearing ledger-style report (Fund Position), a
/// scalar "checks" dashboard (Financial Integrity Dashboard), an empty-rows list report (Booking
/// Revenue), a full-dataset-pagination-loop handler exercised with zero underlying rows (Attendance
/// Register — the loop's termination condition still fires with an empty first page), a zero-param
/// report (Generator Maintenance Due), and a required-date-range report (Consumption Summary).
/// </summary>
public class Mvp1ReportExportRolloutSmokeTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    private readonly PostgresApiFactory _factory;

    public Mvp1ReportExportRolloutSmokeTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> SeedTenantAndRegisterAsync(string slug)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Tenant tenant = Tenant.Provision($"Tenant {slug}", slug, clock.UtcNow);
        tenant.Activate(clock.UtcNow);
        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId = tenant.Id.Value,
            email = $"owner-{slug}@example.com",
            password = "Correct-Horse-Battery-9",
            fullName = "Test Owner",
            phoneNumber = (string?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthTokensDto? tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        tokens.Should().NotBeNull();
        return tokens!.AccessToken;
    }

    private async Task<HttpResponseMessage> ExportAsync(string accessToken, string url)
    {
        using HttpClient client = _factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private async Task AssertCsvAsync(string accessToken, string url, string expectedTitleFragment)
    {
        HttpResponseMessage response = await ExportAsync(accessToken, $"{url}&format=csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"CSV export at {url} should succeed for a fully-permissioned owner");
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(Utf8Bom);
        string csv = Encoding.UTF8.GetString(bytes);
        csv.Should().Contain(expectedTitleFragment);
        csv.Should().Contain("Asia/Dhaka", "every export must stamp its generated-at time in the authoritative Bangladesh timezone");
    }

    private async Task AssertPdfAsync(string accessToken, string url)
    {
        HttpResponseMessage response = await ExportAsync(accessToken, $"{url}&format=pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"PDF export at {url} should succeed for a fully-permissioned owner");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(5).Should().Equal(PdfMagicBytes);
        bytes.Length.Should().BeGreaterThan(1000, "a real Chromium-rendered PDF page is never a few trivial bytes");
    }

    [Fact]
    public async Task FundPosition_Export_Produces_Real_Csv_And_Pdf()
    {
        string token = await SeedTenantAndRegisterAsync("fundpos-export");
        const string url = "/api/v1/reports/finance/fund-position/export?";

        await AssertCsvAsync(token, url, "Fund Position");
        await AssertPdfAsync(token, url);
    }

    [Fact]
    public async Task FinancialIntegrityDashboard_Export_Produces_Real_Csv_And_Pdf()
    {
        string token = await SeedTenantAndRegisterAsync("integrity-export");
        const string url = "/api/v1/finance/integrity-dashboard/export?";

        await AssertCsvAsync(token, url, "Financial Integrity Dashboard");
        await AssertPdfAsync(token, url);
    }

    [Fact]
    public async Task BookingRevenue_Export_Produces_Real_Csv_And_Pdf()
    {
        string token = await SeedTenantAndRegisterAsync("bookingrev-export");
        const string url = "/api/v1/reports/facilities/booking-revenue/export?fromDate=2026-01-01&toDate=2026-12-31";

        await AssertCsvAsync(token, url, "Booking Revenue");
        await AssertPdfAsync(token, url);
    }

    [Fact]
    public async Task AttendanceRegister_Export_Produces_Real_Csv_And_Pdf_With_Zero_Rows()
    {
        string token = await SeedTenantAndRegisterAsync("attendance-export");
        const string url = "/api/v1/attendance-records/export?";

        await AssertCsvAsync(token, url, "Attendance");
        await AssertPdfAsync(token, url);
    }

    [Fact]
    public async Task GeneratorMaintenanceDue_Export_Produces_Real_Csv_And_Pdf()
    {
        string token = await SeedTenantAndRegisterAsync("genmaint-export");
        const string url = "/api/v1/reports/operations/generators/maintenance-due/export?";

        await AssertCsvAsync(token, url, "Maintenance");
        await AssertPdfAsync(token, url);
    }

    [Fact]
    public async Task ConsumptionSummary_Export_Produces_Real_Csv_And_Pdf()
    {
        string token = await SeedTenantAndRegisterAsync("consumption-export");
        const string url = "/api/v1/reports/utilities/consumption-summary/export?fromDate=2026-01-01&toDate=2026-12-31";

        await AssertCsvAsync(token, url, "Consumption");
        await AssertPdfAsync(token, url);
    }
}
