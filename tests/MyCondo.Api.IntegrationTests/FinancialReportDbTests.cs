using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Full-stack, real-Postgres proof that Financial Summary (UX-5) computes its authoritative figures
/// with SQL-side aggregates over real Invoice/Payment rows — not by re-deriving totals from a
/// paginated page. Covers the semantics the discovery report's approval explicitly called out:
/// TotalBilled must include an invoice that became Paid during the period (not just still-open ones),
/// Void invoices must be excluded from TotalBilled, Reversed payments must be excluded from
/// TotalCollected, and TotalOutstanding/the three invoice counts are a current snapshot — NOT
/// constrained to the query's FromDate/ToDate window. Same disclosed Docker-daemon limitation as every
/// other PostgresApiFactory-backed test in this project — written and reviewed for correctness but not
/// executed in the environment they were authored in.
/// </summary>
public class FinancialReportDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public FinancialReportDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Seeded(Guid TenantId, Guid BuildingAId, Guid BuildingBId, string AccessToken);

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

    /// <summary>Seeds an active tenant with two buildings (one flat each) and registers the tenant's
    /// bootstrap user, which is auto-granted every permission including report.financial.view — see
    /// BillingPermissionsDbTests for the same bootstrap convention.</summary>
    /// <summary>Buildings/flats/invoices/payments are RLS-<c>FORCE</c>d (tenant_id-scoped) — writing
    /// them via a DI-resolved scope has no HttpContext/JWT for <c>ITenantContextAccessor</c> to read,
    /// so <c>WITH CHECK</c> rejects the insert. Use <see cref="PostgresApiFactory.CreateDbContextForTenant"/>'s
    /// fixed-tenant-context pattern instead — see <c>.claude/skills/postgresql-rls.md</c> "RLS-safe
    /// seeding". <c>tenancy.tenants</c> itself has no <c>tenant_id</c>/RLS, so <see cref="SeedActiveTenantAsync"/>
    /// is unaffected.</summary>
    private async Task<(Seeded Seeded, FlatId FlatAId, FlatId FlatBId)> SeedTenantWithTwoBuildingsAsync(string slug)
    {
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using IServiceScope clockScope = _factory.Services.CreateScope();
        IClock clock = clockScope.ServiceProvider.GetRequiredService<IClock>();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        BuildingRepository buildings = new(db);
        FlatRepository flats = new(db);

        Building buildingA = Building.Create(tenantId, "Tower A", $"{slug}-A", null, clock.UtcNow);
        Building buildingB = Building.Create(tenantId, "Tower B", $"{slug}-B", null, clock.UtcNow);
        buildings.Add(buildingA);
        buildings.Add(buildingB);

        Flat flatA = Flat.Create(tenantId, buildingA.Id, "A-101", 1, FlatType.Residential, clock.UtcNow);
        Flat flatB = Flat.Create(tenantId, buildingB.Id, "B-101", 1, FlatType.Residential, clock.UtcNow);
        flats.Add(flatA);
        flats.Add(flatB);

        await db.SaveChangesAsync(CancellationToken.None);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"owner-{slug}@example.com");

        return (new Seeded(tenantId, buildingA.Id.Value, buildingB.Id.Value, tokens.AccessToken), flatA.Id, flatB.Id);
    }

    private async Task<InvoiceId> SeedInvoiceAsync(
        Guid tenantId, BuildingId buildingId, FlatId flatId, string invoiceNumber, decimal amount, DateOnly invoiceDate,
        Action<Invoice>? mutate = null)
    {
        using IServiceScope clockScope = _factory.Services.CreateScope();
        IClock clock = clockScope.ServiceProvider.GetRequiredService<IClock>();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        InvoiceRepository invoices = new(db);

        InvoiceLineInput line = new(null, "Service Charge", "Maintenance", "FixedAmount", amount, null, 1, amount, "Test line");
        (Invoice invoice, IReadOnlyList<InvoiceLine> lines) = Invoice.Issue(
            tenantId, buildingId, flatId, invoiceNumber, InvoiceSource.ServiceCharge,
            invoiceDate, invoiceDate, invoiceDate, invoiceDate, [line], LedgerPostingId.New(), clock.UtcNow);

        mutate?.Invoke(invoice);

        invoices.Add(invoice);
        invoices.AddLines(lines);
        await db.SaveChangesAsync(CancellationToken.None);

        return invoice.Id;
    }

    private async Task SeedPaymentAsync(
        Guid tenantId, FlatId flatId, decimal amount, DateOnly businessDate, bool reversed = false)
    {
        using IServiceScope clockScope = _factory.Services.CreateScope();
        IClock clock = clockScope.ServiceProvider.GetRequiredService<IClock>();

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        PaymentRepository payments = new(db);

        Payment payment = Payment.Record(
            tenantId, flatId, amount, PaymentMethod.Cash, "REF-1", businessDate, null, LedgerPostingId.New(), clock.UtcNow);

        if (reversed)
        {
            payment.Reverse("Test reversal", null, clock.UtcNow);
        }

        payments.Add(payment);
        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<FinancialSummaryDto> GetSummaryAsync(string accessToken, DateOnly fromDate, DateOnly toDate, Guid? buildingId = null)
    {
        using HttpClient client = _factory.CreateClient();
        string url = $"/api/v1/reports/financial/summary?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}"
            + (buildingId is Guid b ? $"&buildingId={b}" : string.Empty);

        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", accessToken);
        HttpResponseMessage response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        FinancialSummaryDto? dto = await response.Content.ReadFromJsonAsync<FinancialSummaryDto>(JsonOptions);
        dto.Should().NotBeNull();
        return dto!;
    }

    [Fact]
    public async Task Summary_Reflects_Period_Billed_Collected_And_Snapshot_Outstanding_Correctly()
    {
        (Seeded seeded, FlatId flatAId, _) = await SeedTenantWithTwoBuildingsAsync("fin-summary");
        BuildingId buildingAId = new(seeded.BuildingAId);
        DateOnly periodFrom = new(2026, 8, 1);
        DateOnly periodTo = new(2026, 8, 31);

        // Issued in-period, still open — contributes to TotalBilled and TotalOutstanding.
        await SeedInvoiceAsync(seeded.TenantId, buildingAId, flatAId, "INV-A-1", 1000m, new DateOnly(2026, 8, 5));

        // Issued in-period, fully paid during the period — must still count toward TotalBilled
        // (billed reflects what was invoiced, not what remains unpaid).
        await SeedInvoiceAsync(seeded.TenantId, buildingAId, flatAId, "INV-A-2", 500m, new DateOnly(2026, 8, 10),
            invoice => invoice.ApplyPayment(500m));

        // Issued in-period but voided — excluded from TotalBilled entirely.
        await SeedInvoiceAsync(seeded.TenantId, buildingAId, flatAId, "INV-A-3", 2000m, new DateOnly(2026, 8, 12),
            invoice => invoice.Void("Issued in error", null, LedgerPostingId.New(), DateTimeOffset.UtcNow));

        // Issued OUTSIDE the query window, still open — must NOT contribute to TotalBilled (wrong
        // period) but MUST contribute to TotalOutstanding (a current snapshot, not period-constrained).
        await SeedInvoiceAsync(seeded.TenantId, buildingAId, flatAId, "INV-A-4", 750m, new DateOnly(2026, 6, 1));

        // A posted payment within the period — counted in TotalCollected.
        await SeedPaymentAsync(seeded.TenantId, flatAId, 500m, new DateOnly(2026, 8, 10));

        // A reversed payment within the period — excluded from TotalCollected.
        await SeedPaymentAsync(seeded.TenantId, flatAId, 300m, new DateOnly(2026, 8, 15), reversed: true);

        FinancialSummaryDto summary = await GetSummaryAsync(seeded.AccessToken, periodFrom, periodTo);

        summary.TotalBilled.Should().Be(1000m + 500m, "the voided and out-of-period invoices must be excluded, but the paid one must still count");
        summary.TotalCollected.Should().Be(500m, "the reversed payment must be excluded");
        summary.TotalOutstanding.Should().Be(1000m + 750m, "the out-of-period open invoice must still count — Outstanding is a snapshot, not period-bound");
        summary.UnpaidInvoiceCount.Should().Be(2); // INV-A-1 and INV-A-4
        summary.PartiallyPaidInvoiceCount.Should().Be(0);
    }

    [Fact]
    public async Task Summary_Is_Isolated_By_Building_When_BuildingId_Filter_Applied()
    {
        (Seeded seeded, FlatId flatAId, FlatId flatBId) = await SeedTenantWithTwoBuildingsAsync("fin-building-scope");
        DateOnly periodFrom = new(2026, 8, 1);
        DateOnly periodTo = new(2026, 8, 31);

        await SeedInvoiceAsync(seeded.TenantId, new BuildingId(seeded.BuildingAId), flatAId, "INV-A-1", 1000m, new DateOnly(2026, 8, 5));
        await SeedInvoiceAsync(seeded.TenantId, new BuildingId(seeded.BuildingBId), flatBId, "INV-B-1", 4000m, new DateOnly(2026, 8, 5));
        await SeedPaymentAsync(seeded.TenantId, flatAId, 200m, new DateOnly(2026, 8, 6));
        await SeedPaymentAsync(seeded.TenantId, flatBId, 900m, new DateOnly(2026, 8, 6));

        FinancialSummaryDto buildingASummary = await GetSummaryAsync(seeded.AccessToken, periodFrom, periodTo, seeded.BuildingAId);

        buildingASummary.TotalBilled.Should().Be(1000m, "Building B's invoice must not leak into a Building A-scoped query");
        buildingASummary.TotalCollected.Should().Be(200m, "Building B's payment (via Flat B) must not leak into a Building A-scoped query");
    }

    [Fact]
    public async Task Summary_Does_Not_Leak_Across_Tenants()
    {
        (Seeded tenantOne, FlatId tenantOneFlatId, _) = await SeedTenantWithTwoBuildingsAsync("fin-tenant-one");
        (Seeded tenantTwo, FlatId tenantTwoFlatId, _) = await SeedTenantWithTwoBuildingsAsync("fin-tenant-two");
        DateOnly periodFrom = new(2026, 8, 1);
        DateOnly periodTo = new(2026, 8, 31);

        await SeedInvoiceAsync(tenantOne.TenantId, new BuildingId(tenantOne.BuildingAId), tenantOneFlatId, "T1-INV-1", 1000m, new DateOnly(2026, 8, 5));
        await SeedInvoiceAsync(tenantTwo.TenantId, new BuildingId(tenantTwo.BuildingAId), tenantTwoFlatId, "T2-INV-1", 99_000m, new DateOnly(2026, 8, 5));

        FinancialSummaryDto tenantOneSummary = await GetSummaryAsync(tenantOne.AccessToken, periodFrom, periodTo);

        tenantOneSummary.TotalBilled.Should().Be(1000m, "another tenant's invoice must never appear in this tenant's summary");
    }

    [Fact]
    public async Task Invalid_Date_Range_Returns_400()
    {
        (Seeded seeded, _, _) = await SeedTenantWithTwoBuildingsAsync("fin-invalid-range");

        using HttpClient client = _factory.CreateClient();
        using HttpRequestMessage request = new(
            HttpMethod.Get, "/api/v1/reports/financial/summary?fromDate=2026-08-31&toDate=2026-08-01");
        request.Headers.Authorization = new("Bearer", seeded.AccessToken);

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
