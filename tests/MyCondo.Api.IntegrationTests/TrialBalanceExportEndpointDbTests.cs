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
/// Full-stack, real-Postgres + real-headless-Chromium proof that the report export pipeline
/// (Features/Finance/Reports/Queries/ExportTrialBalance + MyCondo.Infrastructure.Reports) actually
/// produces well-formed CSV/PDF bytes over HTTP — not just a 200 status. No ledger postings are
/// seeded here (Trial Balance with no postings is a valid, already-covered empty-report state per
/// GetTrialBalanceQueryHandlerTests); the DTO→document mapping itself is covered precisely by
/// TrialBalanceExportMapperTests. Needs a Docker daemon (PostgreSQL container) and the Playwright
/// Chromium browser installed on the host running this test (`playwright.ps1 install chromium` from
/// the MyCondo.Api.IntegrationTests build output directory).
/// </summary>
public class TrialBalanceExportEndpointDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    private readonly PostgresApiFactory _factory;

    public TrialBalanceExportEndpointDbTests(PostgresApiFactory factory)
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

    private async Task<HttpResponseMessage> ExportAsync(string accessToken, string format)
    {
        using HttpClient client = _factory.CreateClient();
        using HttpRequestMessage request = new(
            HttpMethod.Get, $"/api/v1/reports/finance/trial-balance/export?format={format}");
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Csv_Export_Returns_Well_Formed_Csv_With_Utf8_Bom_And_Title()
    {
        string accessToken = await SeedTenantAndRegisterAsync("tb-export-csv");

        HttpResponseMessage response = await ExportAsync(accessToken, "csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(Utf8Bom);
        Encoding.UTF8.GetString(bytes).Should().Contain("Trial Balance");
    }

    [Fact]
    public async Task Pdf_Export_Returns_Real_Chromium_Rendered_Pdf_Bytes()
    {
        string accessToken = await SeedTenantAndRegisterAsync("tb-export-pdf");

        HttpResponseMessage response = await ExportAsync(accessToken, "pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(5).Should().Equal(PdfMagicBytes);
        bytes.Length.Should().BeGreaterThan(1000, "a real rendered PDF page is never a few trivial bytes");
    }

    [Fact]
    public async Task Unsupported_Format_Returns_400()
    {
        string accessToken = await SeedTenantAndRegisterAsync("tb-export-badformat");

        HttpResponseMessage response = await ExportAsync(accessToken, "xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthenticated_Request_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/reports/finance/trial-balance/export?format=csv");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
