using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Full-stack, real-Postgres + real-headless-Chromium proof that the Financial Statements export
/// pipeline (Features/Finance/FinancialStatements/Queries/ExportFinancialStatements +
/// MyCondo.Infrastructure.Reports.FinancialStatementsPdfRenderer) actually produces well-formed CSV/PDF
/// bytes over HTTP for the composite Statement of Financial Position + Income &amp; Expenditure + Notes
/// document — not just a 200 status, and never another tenant's ledger activity. Mirrors
/// TrialBalanceExportEndpointDbTests' style. Needs a Docker daemon (PostgreSQL container) and the
/// Playwright Chromium browser installed on the host running this test (`playwright.ps1 install
/// chromium` from the MyCondo.Api.IntegrationTests build output directory).
/// </summary>
public class FinancialStatementsExportEndpointDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();
    private const string ExportUrl = "/api/v1/reports/finance/financial-statements/export";

    private readonly PostgresApiFactory _factory;

    public FinancialStatementsExportEndpointDbTests(PostgresApiFactory factory)
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

    private async Task<string> RegisterAsync(Guid tenantId, string slug)
    {
        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
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

    private async Task<string> SeedTenantAndRegisterAsync(string slug)
    {
        Guid tenantId = await SeedActiveTenantAsync(slug);
        return await RegisterAsync(tenantId, slug);
    }

    /// <summary>Ledger postings are RLS-<c>FORCE</c>d — writing them via a DI-resolved scope has no
    /// HttpContext/JWT for tenant context, so <c>WITH CHECK</c> rejects the insert. Use
    /// <see cref="PostgresApiFactory.CreateDbContextForTenant"/>'s fixed-tenant-context pattern instead,
    /// same as every other Finance DB test in this project.</summary>
    private async Task SeedCashPostingAsync(Guid tenantId, decimal amount, DateOnly businessDate)
    {
        using IServiceScope clockScope = _factory.Services.CreateScope();
        IClock clock = clockScope.ServiceProvider.GetRequiredService<IClock>();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);

        // Codes must not collide with the 16 default accounts /auth/register auto-seeds for a new
        // tenant — a random suffix keeps this independent of that seed list's exact contents.
        ChartOfAccount cash = ChartOfAccount.Create(
            tenantId, $"1{Random.Shared.Next(100, 999)}", "Test Cash & Bank", AccountCategory.Asset, LedgerDirection.Debit,
            statementGroup: FinancialStatementGroup.CashAndBank);
        ChartOfAccount income = ChartOfAccount.Create(
            tenantId, $"4{Random.Shared.Next(100, 999)}", "Test Association Revenue", AccountCategory.Income, LedgerDirection.Credit,
            statementGroup: FinancialStatementGroup.ServiceChargeIncome);
        db.Set<ChartOfAccount>().AddRange(cash, income);

        (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
            tenantId, businessDate, "Export isolation test posting", "Test", null,
            [
                new LedgerLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, amount, "Debit leg"),
                new LedgerLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, amount, "Credit leg"),
            ],
            clock.UtcNow);

        entries[0].SetFinanceDimensions(cash.Id, null, null);
        entries[1].SetFinanceDimensions(income.Id, null, null);

        db.Set<LedgerPosting>().Add(posting);
        db.Set<LedgerEntry>().AddRange(entries);

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<HttpResponseMessage> ExportAsync(
        string accessToken, string format, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        DateOnly start = startDate ?? new DateOnly(2026, 6, 1);
        DateOnly end = endDate ?? new DateOnly(2026, 6, 30);

        using HttpClient client = _factory.CreateClient();
        using HttpRequestMessage request = new(
            HttpMethod.Get, $"{ExportUrl}?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}&format={format}");
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Csv_Export_Returns_Well_Formed_Csv_With_Utf8_Bom_And_Title()
    {
        string accessToken = await SeedTenantAndRegisterAsync("fs-export-csv");

        HttpResponseMessage response = await ExportAsync(accessToken, "csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(Utf8Bom);
        string text = Encoding.UTF8.GetString(bytes);
        text.Should().Contain("Financial Statements");
        text.Should().Contain("Reporting Period");
    }

    [Fact]
    public async Task Pdf_Export_Returns_Real_Chromium_Rendered_Pdf_Bytes()
    {
        Guid tenantId = await SeedActiveTenantAsync("fs-export-pdf");
        string accessToken = await RegisterAsync(tenantId, "fs-export-pdf");
        await SeedCashPostingAsync(tenantId, 250_000m, new DateOnly(2026, 6, 15));

        HttpResponseMessage response = await ExportAsync(accessToken, "pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(5).Should().Equal(PdfMagicBytes);
        bytes.Length.Should().BeGreaterThan(1000, "a real rendered multi-section PDF is never a few trivial bytes");
    }

    [Fact]
    public async Task Filename_Follows_The_StartDate_To_EndDate_Convention()
    {
        string accessToken = await SeedTenantAndRegisterAsync("fs-export-filename");

        HttpResponseMessage response = await ExportAsync(
            accessToken, "csv", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? string.Empty;
        fileName.Trim('"').Should().Be("financial-statements-2026-03-01-to-2026-03-31.csv");
    }

    [Fact]
    public async Task Unsupported_Format_Returns_400()
    {
        string accessToken = await SeedTenantAndRegisterAsync("fs-export-badformat");

        HttpResponseMessage response = await ExportAsync(accessToken, "xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invalid_Date_Range_Returns_400()
    {
        string accessToken = await SeedTenantAndRegisterAsync("fs-export-badrange");

        HttpResponseMessage response = await ExportAsync(
            accessToken, "csv", new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unauthenticated_Request_Returns_401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"{ExportUrl}?startDate=2026-06-01&endDate=2026-06-30&format=csv");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Export_Does_Not_Reflect_Another_Tenants_Ledger_Activity()
    {
        Guid tenantAId = await SeedActiveTenantAsync("fs-export-tenant-a");
        Guid tenantBId = await SeedActiveTenantAsync("fs-export-tenant-b");
        string tenantAToken = await RegisterAsync(tenantAId, "fs-export-tenant-a");

        // Tenant B has real cash/income activity in the exported window; tenant A has none. If isolation
        // ever broke, tenant A's Total Assets would stop being zero and/or this distinctive amount would
        // leak into tenant A's CSV.
        await SeedCashPostingAsync(tenantBId, 999_999m, new DateOnly(2026, 6, 15));

        HttpResponseMessage response = await ExportAsync(tenantAToken, "csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        string text = Encoding.UTF8.GetString(bytes);

        text.Should().NotContain("999,999.00", "another tenant's ledger activity must never appear in this tenant's export");
        text.Should().Contain("Total Assets,0.00", "a tenant with no ledger activity must show zero assets, not another tenant's balance");
    }
}
