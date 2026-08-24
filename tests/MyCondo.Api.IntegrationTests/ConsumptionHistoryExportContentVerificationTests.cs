using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Domain.Features.Utilities.Readings;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// DB-backed regression coverage for the Consumption History report export — the one report in the
/// MVP-1 rollout whose on-screen source (<c>GetReadingsQuery</c>) is capped at a 100-row UI page.
/// Seeds 105 readings for a single meter (deliberately &gt; the 100-row UI cap and the export handler's
/// own 100-row page-loop threshold) directly via the <see cref="Reading.Record"/> domain factory — the
/// same "seed via the real domain factory, not raw SQL" convention <see cref="ExportContentVerificationTests"/>
/// uses for ledger entries — then verifies, against the real CSV/PDF bytes produced over HTTP, that the
/// export returns every reading (not just the 100 the screen would show), resolves the meter/building to
/// human-readable labels instead of GUIDs, and is scoped to the selected meter only.
/// </summary>
public class ConsumptionHistoryExportContentVerificationTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    private readonly PostgresApiFactory _factory;

    public ConsumptionHistoryExportContentVerificationTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConsumptionHistory_Export_Returns_Full_Dataset_Beyond_The_100_Row_UI_Cap_With_No_Guid_Leakage()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Tenant tenant = Tenant.Provision("Tenant consumption-history-verify", "consumption-history-verify", clock.UtcNow);
        tenant.Activate(clock.UtcNow);
        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        Guid tenantId = tenant.Id.Value;

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        BuildingRepository buildings = new(db);
        FlatRepository flats = new(db);
        MeterRepository meters = new(db);
        ReadingRepository readings = new(db);

        Building building = Building.Create(tenantId, "Lakeview Towers", "LVT", null, clock.UtcNow);
        buildings.Add(building);
        Flat flat = Flat.Create(tenantId, building.Id, "B-201", 1, FlatType.Residential, clock.UtcNow);
        flats.Add(flat);
        Meter meter = Meter.Install(tenantId, building.Id, UtilityType.Electricity, "MTR-CH-001", clock.UtcNow);
        meters.Add(meter);
        await db.SaveChangesAsync(CancellationToken.None);

        // 105 readings — beyond both the on-screen page's 100-row cap and the export handler's own
        // 100-row page-loop threshold — for the SAME meter, so a naive single-page fetch would truncate.
        const int readingCount = 105;
        DateOnly start = new(2016, 1, 1);
        for (int i = 0; i < readingCount; i++)
        {
            DateOnly periodStart = start.AddMonths(i);
            DateOnly periodEnd = start.AddMonths(i + 1);
            Reading reading = Reading.Record(
                tenantId, meter.Id, flat.Id, UtilityType.Electricity, building.Id,
                periodStart, periodEnd, previousReading: i * 10m, presentReading: (i + 1) * 10m,
                readingDate: periodEnd, overrideReason: null, correctsReadingId: null, clock.UtcNow);
            readings.Add(reading);
        }

        await db.SaveChangesAsync(CancellationToken.None);

        using HttpClient registerClient = _factory.CreateClient();
        HttpResponseMessage registerResponse = await registerClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email = "owner-consumption-history-verify@example.com",
            password = "Correct-Horse-Battery-9",
            fullName = "Test Owner",
            phoneNumber = (string?)null,
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? tokens = await registerResponse.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        tokens.Should().NotBeNull();
        string token = tokens!.AccessToken;

        // ---- Confirm the on-screen source really does cap at 100, proving the export gap is real ----
        using HttpClient screenClient = _factory.CreateClient();
        using HttpRequestMessage screenRequest = new(
            HttpMethod.Get, $"/api/v1/readings?meterId={meter.Id.Value}&page=1&pageSize=100");
        screenRequest.Headers.Authorization = new("Bearer", token);
        HttpResponseMessage screenResponse = await screenClient.SendAsync(screenRequest);
        screenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<ReadingDto>? screenPage = await screenResponse.Content.ReadFromJsonAsync<PagedResult<ReadingDto>>(JsonOptions);
        screenPage.Should().NotBeNull();
        screenPage!.Items.Count.Should().Be(100, "the on-screen list is capped at a 100-row page");
        screenPage.Total.Should().Be(readingCount, "the server knows about all 105 readings even though only 100 are shown");

        // ---- CSV export: full dataset, human-readable metadata, no GUIDs ----
        using HttpClient csvClient = _factory.CreateClient();
        using HttpRequestMessage csvRequest = new(
            HttpMethod.Get, $"/api/v1/reports/utilities/consumption-history/export?meterId={meter.Id.Value}&format=csv");
        csvRequest.Headers.Authorization = new("Bearer", token);
        HttpResponseMessage csvResponse = await csvClient.SendAsync(csvRequest);
        csvResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        csvResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        byte[] csvBytes = await csvResponse.Content.ReadAsByteArrayAsync();
        csvBytes.Take(3).Should().Equal(Utf8Bom);
        string csv = Encoding.UTF8.GetString(csvBytes);

        csv.Should().Contain("Electricity Consumption History");
        csv.Should().Contain("Lakeview Towers", "the building's real name must appear, not its GUID");
        csv.Should().Contain("MTR-CH-001", "the meter's human-readable number must appear");
        csv.Should().Contain("Asia/Dhaka");
        csv.Should().NotContain(building.Id.Value.ToString(), "no Building GUID leaks into the export");
        csv.Should().NotContain(meter.Id.Value.ToString(), "no Meter GUID leaks into the export");
        csv.Should().NotContain(flat.Id.Value.ToString(), "no Flat GUID leaks into the export");

        string[] dataLines = csv.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        int headerLineIndex = Array.IndexOf(dataLines, "Period Start,Period End,Consumption (Units),Status,Abnormal");
        headerLineIndex.Should().BeGreaterThan(0, "the exact expected CSV column header row must be present");
        int rowCount = dataLines.Length - headerLineIndex - 1;
        rowCount.Should().Be(readingCount, "the export must return every one of the 105 readings, not just the 100-row UI page");

        string firstDataRow = dataLines[headerLineIndex + 1];
        string lastDataRow = dataLines[^1];
        firstDataRow.Should().StartWith("2016-01-01,2016-02-01,", "rows must be sorted ascending by period, earliest first");
        lastDataRow.Should().StartWith(
            $"{start.AddMonths(readingCount - 1):yyyy-MM-dd},{start.AddMonths(readingCount):yyyy-MM-dd},",
            "the last row must be the most recent period");

        string filename = csvResponse.Content.Headers.ContentDisposition?.FileNameStar
            ?? csvResponse.Content.Headers.ContentDisposition?.FileName
            ?? string.Empty;
        filename.Should().Contain("consumption-history");

        // ---- PDF export: real Chromium-rendered bytes, same 105-row dataset ----
        using HttpClient pdfClient = _factory.CreateClient();
        using HttpRequestMessage pdfRequest = new(
            HttpMethod.Get, $"/api/v1/reports/utilities/consumption-history/export?meterId={meter.Id.Value}&format=pdf");
        pdfRequest.Headers.Authorization = new("Bearer", token);
        HttpResponseMessage pdfResponse = await pdfClient.SendAsync(pdfRequest);
        pdfResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        pdfResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        byte[] pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        pdfBytes.Take(5).Should().Equal(PdfMagicBytes);
        pdfBytes.Length.Should().BeGreaterThan(1000, "a real multi-page Chromium-rendered PDF is never a few trivial bytes");
    }
}
