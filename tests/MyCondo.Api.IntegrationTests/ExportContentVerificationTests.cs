using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Finance.Reports.Queries.GetAccountLedger;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialOverview;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFinancialPosition;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFlatFinancialStatement;
using MyCondo.Application.Features.Finance.Reports.Queries.GetGeneralLedger;
using MyCondo.Application.Features.Finance.Reports.Queries.GetResidentFinancialStatementReport;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// DB-backed regression coverage for the MVP-1 Report Export Rollout's 8 highest-risk (paginated /
/// running-balance) reports. Seeds real, sizeable ledger activity (well beyond every export handler's
/// internal page-loop threshold) via the same <see cref="IFinancialPostingService"/> every production
/// posting path uses, then verifies — against the real CSV/PDF bytes produced over HTTP — that: (1) the
/// export's row count matches the server-reported Total (never truncated to one page), (2)
/// opening/running/closing balances recomputed independently from the CSV are internally consistent
/// across concatenated pages, (3) totals match the underlying GET query's own DTO fields, (4) no raw
/// GUID leaks into a human-facing scope/flat field, and (5) real Chromium-rendered PDF bytes are
/// produced. This suite is what caught <see cref="LedgerEntryRepository.SearchForFlatChronologicalAsync"/>
/// / <see cref="LedgerEntryRepository.SearchForFlatAsync"/> throwing an untranslatable-LINQ 500 on every
/// call — a pre-existing defect no mocked unit test could see — so it is kept as permanent coverage
/// alongside <see cref="Mvp1ReportExportRolloutSmokeTests"/> rather than deleted after the pass that
/// found it.
/// </summary>
public class ExportContentVerificationTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] PdfMagicBytes = "%PDF-"u8.ToArray();

    private readonly PostgresApiFactory _factory;

    public ExportContentVerificationTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Seeded(Guid TenantId, Guid BuildingId, string AccessToken);

    private sealed record ParsedCsv(
        string Title,
        Dictionary<string, string> Metadata,
        string GeneratedAt,
        List<string> Headers,
        List<List<string>> Rows,
        Dictionary<string, string> Totals);

    // ---------- Seeding ----------

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

    private async Task<AuthTokensDto> RegisterAsync(HttpClient client, Guid tenantId, string email)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email,
            password = "Correct-Horse-Battery-9",
            fullName = "Test Owner",
            phoneNumber = (string?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await _factory.GrantFullEntitlementToAllTenantsAsync();

        AuthTokensDto? tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions);
        tokens.Should().NotBeNull();
        return tokens!;
    }

    private async Task<(Seeded Seeded, FlatId FlatAId, FlatId FlatBId)> SeedTenantWithBuildingAndTwoFlatsAsync(string slug, string buildingName)
    {
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using IServiceScope clockScope = _factory.Services.CreateScope();
        IClock clock = clockScope.ServiceProvider.GetRequiredService<IClock>();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        BuildingRepository buildings = new(db);
        FlatRepository flats = new(db);

        Building building = Building.Create(tenantId, buildingName, $"{slug}-A", null, clock.UtcNow);
        buildings.Add(building);

        Flat flatA = Flat.Create(tenantId, building.Id, "A-101", 1, FlatType.Residential, clock.UtcNow);
        Flat flatB = Flat.Create(tenantId, building.Id, "A-102", 2, FlatType.Residential, clock.UtcNow);
        flats.Add(flatA);
        flats.Add(flatB);

        await db.SaveChangesAsync(CancellationToken.None);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"owner-{slug}@example.com");

        return (new Seeded(tenantId, building.Id.Value, tokens.AccessToken), flatA.Id, flatB.Id);
    }

    /// <summary>Posts real, balanced ledger activity via <see cref="IFinancialPostingService"/> — the
    /// same centralized posting engine every command handler (RecordPayment, etc.) uses — rather than
    /// hand-crafting <see cref="LedgerEntry"/> rows, so the seeded data exercises the real account-mapping
    /// resolution path. 130 daily charges + 90 interleaved payments for Flat A (220 ResidentReceivable
    /// entries — over every export handler's page-loop threshold of 100/200) plus 5 charges for Flat B
    /// (cross-flat isolation check). Returns the tenant's resolved ResidentReceivable chart-of-account id
    /// for the Account Ledger test.</summary>
    private async Task<Guid> SeedLedgerActivityAsync(Guid tenantId, FlatId flatA, FlatId flatB)
    {
        using IServiceScope clockScope = _factory.Services.CreateScope();
        IClock clock = clockScope.ServiceProvider.GetRequiredService<IClock>();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        FinancialPostingService posting = new(
            new AccountMappingRepository(db),
            new ChartOfAccountRepository(db),
            new AccountingPeriodRepository(db),
            new LedgerPostingRepository(db),
            new LedgerEntryRepository(db),
            clock,
            NullLogger<FinancialPostingService>.Instance);

        DateOnly start = new(2025, 9, 1);

        for (int i = 0; i < 130; i++)
        {
            DateOnly date = start.AddDays(i);
            await posting.PostAsync(
                new FinancialPostingRequest(tenantId, date, $"Service charge {date:yyyy-MM-dd}", "TestCharge", null,
                [
                    new FinancialPostingLine(LedgerAccountType.ResidentReceivable, flatA, LedgerDirection.Debit, 100m),
                    new FinancialPostingLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, 100m),
                ]),
                CancellationToken.None);
        }

        for (int i = 0; i < 90; i++)
        {
            DateOnly date = start.AddDays(i).AddDays(1);
            await posting.PostAsync(
                new FinancialPostingRequest(tenantId, date, $"Payment received {date:yyyy-MM-dd}", "TestPayment", null,
                [
                    new FinancialPostingLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Debit, 80m),
                    new FinancialPostingLine(LedgerAccountType.ResidentReceivable, flatA, LedgerDirection.Credit, 80m),
                ]),
                CancellationToken.None);
        }

        for (int i = 0; i < 5; i++)
        {
            DateOnly date = start.AddDays(i * 10);
            await posting.PostAsync(
                new FinancialPostingRequest(tenantId, date, $"Flat B charge {date:yyyy-MM-dd}", "TestCharge", null,
                [
                    new FinancialPostingLine(LedgerAccountType.ResidentReceivable, flatB, LedgerDirection.Debit, 500m),
                    new FinancialPostingLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, 500m),
                ]),
                CancellationToken.None);
        }

        await db.SaveChangesAsync(CancellationToken.None);

        AccountMappingRepository mappings = new(db);
        Guid residentReceivableAccountId = (await mappings.ResolveAccountIdAsync(
            tenantId, nameof(LedgerAccountType.ResidentReceivable), CancellationToken.None))!.Value.Value;

        return residentReceivableAccountId;
    }

    private async Task SeedInvoiceAsync(
        Guid tenantId, BuildingId buildingId, FlatId flatId, string invoiceNumber, decimal totalAmount, DateOnly dueDate,
        decimal amountPaid = 0m)
    {
        using IServiceScope clockScope = _factory.Services.CreateScope();
        IClock clock = clockScope.ServiceProvider.GetRequiredService<IClock>();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        InvoiceRepository invoices = new(db);

        InvoiceLineInput line = new(null, "Service Charge", "Maintenance", "FixedAmount", totalAmount, null, 1, totalAmount, "Test line");
        (Invoice invoice, IReadOnlyList<InvoiceLine> lines) = Invoice.Issue(
            tenantId, buildingId, flatId, invoiceNumber, InvoiceSource.ServiceCharge,
            dueDate, dueDate, dueDate, dueDate, [line], LedgerPostingId.New(),
            LedgerAccountType.AssociationRevenue, null, clock.UtcNow);

        if (amountPaid > 0)
        {
            invoice.ApplyPayment(amountPaid);
        }

        invoices.Add(invoice);
        invoices.AddLines(lines);
        await db.SaveChangesAsync(CancellationToken.None);
    }

    // ---------- HTTP + CSV helpers ----------

    private async Task<T> GetJsonAsync<T>(string accessToken, string url)
    {
        using HttpClient client = _factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", accessToken);
        HttpResponseMessage response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET {url} should succeed for a fully-permissioned owner");

        T? dto = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        dto.Should().NotBeNull();
        return dto!;
    }

    private async Task<(string Csv, string FileName)> ExportCsvAsync(string accessToken, string url)
    {
        using HttpClient client = _factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, $"{url}{(url.Contains('?') ? "&" : "?")}format=csv");
        request.Headers.Authorization = new("Bearer", accessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"CSV export at {url} should succeed");
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(Utf8Bom);

        return (Encoding.UTF8.GetString(bytes), response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? string.Empty);
    }

    private async Task<byte[]> ExportPdfAsync(string accessToken, string url)
    {
        using HttpClient client = _factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, $"{url}{(url.Contains('?') ? "&" : "?")}format=pdf");
        request.Headers.Authorization = new("Bearer", accessToken);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"PDF export at {url} should succeed");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(5).Should().Equal(PdfMagicBytes);
        bytes.Length.Should().BeGreaterThan(1000);

        return bytes;
    }

    /// <summary>Minimal RFC4180-respecting field splitter matching <c>CsvReportRenderer.EscapeCell</c>'s
    /// quoting (quotes a field iff it contains '"', ',', '\n' or '\r'; doubles embedded quotes) — a naive
    /// <c>string.Split(',')</c> would misparse any "N2"-formatted amount ≥ 1000 (e.g. "1,900.00", which
    /// EscapeCell quotes because of the thousands-separator comma).</summary>
    private static List<string> SplitCsvRow(string line)
    {
        List<string> fields = [];
        StringBuilder current = new();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static ParsedCsv ParseCsv(string csv)
    {
        string[] lines = csv.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        List<List<string>> blocks = [];
        List<string> current = [];
        foreach (string line in lines)
        {
            if (line.Length == 0)
            {
                blocks.Add(current);
                current = [];
            }
            else
            {
                current.Add(line);
            }
        }

        blocks.Add(current);

        List<string> headerBlock = blocks[0];
        string title = headerBlock[0];
        string generatedAt = string.Empty;
        Dictionary<string, string> metadata = [];
        for (int i = 1; i < headerBlock.Count; i++)
        {
            List<string> parts = SplitCsvRow(headerBlock[i]);
            string label = parts[0];
            string value = parts.Count > 1 ? parts[1] : string.Empty;
            if (label == "Generated At")
            {
                generatedAt = value;
            }
            else
            {
                metadata[label] = value;
            }
        }

        List<string> tableBlock = blocks[1];
        List<string> headers = SplitCsvRow(tableBlock[0]);
        List<List<string>> rows = tableBlock.Skip(1).Select(SplitCsvRow).ToList();

        Dictionary<string, string> totals = [];
        if (blocks.Count > 2)
        {
            foreach (string line in blocks[2])
            {
                List<string> parts = SplitCsvRow(line);
                totals[parts[0]] = parts.Count > 1 ? parts[1] : string.Empty;
            }
        }

        return new ParsedCsv(title, metadata, generatedAt, headers, rows, totals);
    }

    private static decimal ParseAmount(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    // ---------- Tests ----------

    [Fact]
    public async Task LedgerBasedReports_Export_Complete_Dataset_With_Correct_Balances_And_No_Guid_Leakage()
    {
        (Seeded seeded, FlatId flatA, FlatId flatB) = await SeedTenantWithBuildingAndTwoFlatsAsync("export-verify", "Lakeview Towers");
        Guid residentReceivableAccountId = await SeedLedgerActivityAsync(seeded.TenantId, flatA, flatB);
        string token = seeded.AccessToken;
        string flatAGuid = flatA.Value.ToString();
        string flatBGuid = flatB.Value.ToString();

        // ---- General Ledger: full tenant history, no filters ----
        GeneralLedgerReportDto glTotalsProbe = await GetJsonAsync<GeneralLedgerReportDto>(
            token, "/api/v1/reports/finance/general-ledger?page=1&pageSize=1");
        (string glCsv, string glFileName) = await ExportCsvAsync(
            token, "/api/v1/reports/finance/general-ledger/export");
        await ExportPdfAsync(token, "/api/v1/reports/finance/general-ledger/export");

        ParsedCsv gl = ParseCsv(glCsv);
        gl.Rows.Count.Should().Be((int)glTotalsProbe.Total, "the export must return every matching row, not just one page");
        glTotalsProbe.Total.Should().BeGreaterThan(200, "the seed must exceed the export handler's 200-row page-loop threshold to prove non-truncation");
        decimal glDebit = gl.Rows.Where(r => r[5] == "Debit").Sum(r => ParseAmount(r[6]));
        decimal glCredit = gl.Rows.Where(r => r[5] == "Credit").Sum(r => ParseAmount(r[6]));
        glDebit.Should().Be(glCredit, "every posting balances, so the tenant-wide General Ledger must always sum to zero net");
        ParseAmount(gl.Totals["Total Debit"]).Should().Be(glDebit);
        ParseAmount(gl.Totals["Total Credit"]).Should().Be(glCredit);
        gl.GeneratedAt.Should().Contain("Asia/Dhaka");
        glCsv.Should().NotContain(flatAGuid).And.NotContain(flatBGuid, "General Ledger exposes no per-flat GUID column");
        glFileName.Should().Contain("general-ledger");

        // ---- Account Ledger (ResidentReceivable): full history across both flats ----
        AccountLedgerReportDto alTotalsProbe = await GetJsonAsync<AccountLedgerReportDto>(
            token, $"/api/v1/reports/finance/account-ledger/{residentReceivableAccountId}?page=1&pageSize=1");
        (string alCsv, _) = await ExportCsvAsync(
            token, $"/api/v1/reports/finance/account-ledger/{residentReceivableAccountId}/export");
        await ExportPdfAsync(token, $"/api/v1/reports/finance/account-ledger/{residentReceivableAccountId}/export");

        ParsedCsv al = ParseCsv(alCsv);
        al.Rows.Count.Should().Be((int)alTotalsProbe.Total);
        alTotalsProbe.Total.Should().BeGreaterThan(200, "225 ResidentReceivable entries (Flat A + Flat B) must exceed the 200-row page-loop threshold");
        AssertRunningBalanceIsInternallyConsistent(al.Rows, amountIndex: 4, runningBalanceIndex: 5, ParseAmount(al.Totals["Opening Balance"]));
        ParseAmount(al.Totals["Closing Balance"]).Should().Be(ParseAmount(al.Rows[^1][5]), "the CSV's own last row must agree with its own Closing Balance total");
        al.Metadata["Account"].Should().NotBeNullOrEmpty();
        alCsv.Should().NotContain(flatAGuid).And.NotContain(flatBGuid);

        // ---- Flat Financial Statement: full history proves non-truncation ----
        FlatFinancialStatementReportDto ffsTotalsProbe = await GetJsonAsync<FlatFinancialStatementReportDto>(
            token, $"/api/v1/reports/finance/flat-statement/{flatA.Value}?page=1&pageSize=1");
        (string ffsCsv, string ffsFileName) = await ExportCsvAsync(
            token, $"/api/v1/reports/finance/flat-statement/{flatA.Value}/export");
        await ExportPdfAsync(token, $"/api/v1/reports/finance/flat-statement/{flatA.Value}/export");

        ParsedCsv ffs = ParseCsv(ffsCsv);
        ffs.Rows.Count.Should().Be((int)ffsTotalsProbe.Total);
        ffsTotalsProbe.Total.Should().Be(220, "Flat A has 130 charges + 90 payments, all tagged ResidentReceivable");
        ffsTotalsProbe.Total.Should().BeGreaterThan(100, "must exceed the Flat Statement export handler's 100-row page-loop threshold");
        AssertRunningBalanceIsInternallyConsistent(ffs.Rows, amountIndex: 4, runningBalanceIndex: 5, openingBalance: 0m);
        ParseAmount(ffs.Totals["Closing Balance"]).Should().Be(ParseAmount(ffs.Rows[^1][5]));
        ffs.Metadata["Flat"].Should().Be("A-101");
        ffsCsv.Should().NotContain(flatAGuid).And.NotContain(flatBGuid, "no Flat GUID leaks — only the human FlatNumber is shown");
        ffsFileName.Should().Contain("flat-statement").And.Contain("A-101");

        // ---- Flat Financial Statement: windowed, verifies opening/closing balance arithmetic ----
        (string ffsWindowCsv, _) = await ExportCsvAsync(
            token,
            $"/api/v1/reports/finance/flat-statement/{flatA.Value}/export?fromDate=2025-12-01");
        ParsedCsv ffsWindow = ParseCsv(ffsWindowCsv);
        decimal openingBalance = ParseAmount(ffsWindow.Totals["Opening Balance"]);
        decimal periodDebit = ParseAmount(ffsWindow.Totals["Period Debit Total"]);
        decimal periodCredit = ParseAmount(ffsWindow.Totals["Period Credit Total"]);
        decimal closingBalance = ParseAmount(ffsWindow.Totals["Closing Balance"]);
        openingBalance.Should().BeGreaterThan(0m, "activity before 2025-12-01 must roll into a non-zero opening balance");
        (openingBalance + periodDebit - periodCredit).Should().Be(closingBalance, "Opening + Period Debit - Period Credit must equal Closing Balance");
        ffsWindow.Rows.All(r => DateOnly.Parse(r[0], CultureInfo.InvariantCulture) >= new DateOnly(2025, 12, 1)).Should().BeTrue("every exported row must respect the FromDate filter");

        // ---- Resident Financial Statement: full history, same underlying ledger data ----
        (string rfsCsv, string rfsFileName) = await ExportCsvAsync(
            token, $"/api/v1/reports/finance/resident-statement/{flatA.Value}/export");
        await ExportPdfAsync(token, $"/api/v1/reports/finance/resident-statement/{flatA.Value}/export");

        ParsedCsv rfs = ParseCsv(rfsCsv);
        rfs.Rows.Count.Should().Be(220);
        AssertRunningBalanceIsInternallyConsistent(rfs.Rows, amountIndex: 4, runningBalanceIndex: 5, openingBalance: 0m);
        ParseAmount(rfs.Totals["Closing Balance"]).Should().Be(ParseAmount(rfs.Rows[^1][5]));
        rfs.Metadata["Flat"].Should().Be("A-101");
        rfsCsv.Should().NotContain(flatAGuid).And.NotContain(flatBGuid);
        rfsFileName.Should().Contain("resident-statement").And.Contain("A-101");

        // ---- Resident Ledger (via resident-accounts endpoint): full history + a reference-type filter ----
        (string rlCsv, string rlFileName) = await ExportCsvAsync(
            token, $"/api/v1/resident-accounts/{flatA.Value}/ledger-entries/export");
        await ExportPdfAsync(token, $"/api/v1/resident-accounts/{flatA.Value}/ledger-entries/export");

        ParsedCsv rl = ParseCsv(rlCsv);
        rl.Rows.Count.Should().Be(220, "Resident Ledger for Flat A must include every one of its 220 ResidentReceivable entries, not just the first page (100-row loop threshold)");
        rlCsv.Should().NotContain(flatAGuid).And.NotContain(flatBGuid);
        rlFileName.Should().Contain("resident-ledger");

        (string rlFilteredCsv, _) = await ExportCsvAsync(
            token, $"/api/v1/resident-accounts/{flatA.Value}/ledger-entries/export?referenceType=TestPayment");
        ParsedCsv rlFiltered = ParseCsv(rlFilteredCsv);
        rlFiltered.Rows.Count.Should().Be(90, "the referenceType=TestPayment filter must return exactly the 90 seeded payment postings");
        rlFiltered.Rows.Should().OnlyContain(r => r[2] == "TestPayment");

        // ---- Financial Position: balance-sheet identity must hold from real trial-balance data ----
        FinancialPositionReportDto fpDto = await GetJsonAsync<FinancialPositionReportDto>(
            token, "/api/v1/reports/finance/financial-position?asOfDate=2026-02-01");
        (string fpCsv, _) = await ExportCsvAsync(token, "/api/v1/reports/finance/financial-position/export?asOfDate=2026-02-01");
        await ExportPdfAsync(token, "/api/v1/reports/finance/financial-position/export?asOfDate=2026-02-01");

        ParsedCsv fp = ParseCsv(fpCsv);
        ParseAmount(fp.Totals["Total Assets"]).Should().Be(fpDto.Assets.Total);
        ParseAmount(fp.Totals["Total Liabilities"]).Should().Be(fpDto.Liabilities.Total);
        ParseAmount(fp.Totals["Total Equity"]).Should().Be(fpDto.Equity.Total);
        ParseAmount(fp.Totals["Retained Surplus/Deficit"]).Should().Be(fpDto.RetainedSurplusDeficit);
        ParseAmount(fp.Totals["Total Liabilities & Equity"]).Should().Be(fpDto.TotalLiabilitiesAndEquity);
        ParseAmount(fp.Totals["Total Assets"]).Should().Be(
            ParseAmount(fp.Totals["Total Liabilities & Equity"]), "the balance sheet must balance: Assets = Liabilities + Equity + Retained Surplus/Deficit");

        // ---- Financial Overview: composite figures must match the same trial-balance source ----
        FinancialOverviewReportDto foDto = await GetJsonAsync<FinancialOverviewReportDto>(
            token, "/api/v1/reports/finance/overview?asOfDate=2026-02-01");
        (string foCsv, _) = await ExportCsvAsync(token, "/api/v1/reports/finance/overview/export?asOfDate=2026-02-01");
        await ExportPdfAsync(token, "/api/v1/reports/finance/overview/export?asOfDate=2026-02-01");

        ParsedCsv fo = ParseCsv(foCsv);
        ParseAmount(fo.Totals["Income Total"]).Should().Be(foDto.IncomeTotal);
        ParseAmount(fo.Totals["Expense Total"]).Should().Be(foDto.ExpenseTotal);
        ParseAmount(fo.Totals["Surplus/Deficit"]).Should().Be(foDto.SurplusDeficit);
        ParseAmount(fo.Totals["Receivables"]).Should().Be(foDto.Receivables);
        ParseAmount(fo.Totals["Cash In Hand"]).Should().Be(foDto.CashPosition.CashInHand);
        ParseAmount(fo.Totals["Bank Balance"]).Should().Be(foDto.CashPosition.BankBalance);
        foDto.IncomeTotal.Should().Be(130 * 100m + 5 * 500m, "Income must equal every AssociationRevenue credit posted (Flat A + Flat B charges)");
        foDto.Receivables.Should().Be(130 * 100m - 90 * 80m + 5 * 500m, "Receivables must equal the net ResidentReceivable balance across both flats");
    }

    [Fact]
    public async Task ReceivablesAgeing_Export_Resolves_Building_Name_And_Buckets_By_Remaining_Balance()
    {
        (Seeded seeded, FlatId flatA, _) = await SeedTenantWithBuildingAndTwoFlatsAsync("ageing-verify", "Sunrise Residency");
        string token = seeded.AccessToken;
        BuildingId buildingId = new(seeded.BuildingId);
        DateOnly asOfDate = new(2026, 2, 15);

        await SeedInvoiceAsync(seeded.TenantId, buildingId, flatA, "INV-1", 1000m, dueDate: new DateOnly(2025, 12, 1), amountPaid: 400m);
        await SeedInvoiceAsync(seeded.TenantId, buildingId, flatA, "INV-2", 250m, dueDate: new DateOnly(2026, 3, 1));

        ReceivablesAgeingReportDto dto = await GetJsonAsync<ReceivablesAgeingReportDto>(
            token, $"/api/v1/reports/financial/receivables-ageing?asOfDate={asOfDate:yyyy-MM-dd}&buildingId={seeded.BuildingId}");
        (string csv, _) = await ExportCsvAsync(
            token, $"/api/v1/reports/financial/receivables-ageing/export?asOfDate={asOfDate:yyyy-MM-dd}&buildingId={seeded.BuildingId}");
        await ExportPdfAsync(token, $"/api/v1/reports/financial/receivables-ageing/export?asOfDate={asOfDate:yyyy-MM-dd}&buildingId={seeded.BuildingId}");

        ParsedCsv parsed = ParseCsv(csv);
        parsed.Metadata["Scope"].Should().Be("Sunrise Residency", "the building's real name must appear, not its GUID");
        csv.Should().NotContain(seeded.BuildingId.ToString());
        decimal bucketSum = parsed.Rows.Sum(r => ParseAmount(r[2]));
        bucketSum.Should().Be(ParseAmount(parsed.Totals["Grand Total"]));
        ParseAmount(parsed.Totals["Grand Total"]).Should().Be(dto.GrandTotal);
    }

    private static void AssertRunningBalanceIsInternallyConsistent(
        List<List<string>> rows, int amountIndex, int runningBalanceIndex, decimal openingBalance)
    {
        int directionIndex = amountIndex - 1;
        decimal running = openingBalance;
        foreach (List<string> row in rows)
        {
            decimal amount = ParseAmount(row[amountIndex]);
            running += row[directionIndex] == "Debit" ? amount : -amount;
            ParseAmount(row[runningBalanceIndex]).Should().Be(running,
                "each row's Running Balance must be exactly the prior balance plus/minus this row's signed amount, " +
                "even across a page boundary the export handler concatenated internally");
        }
    }
}
