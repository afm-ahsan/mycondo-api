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
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Full-stack, real-Postgres proof that Receivables Ageing (UX-5) buckets by remaining balance (not
/// the invoice's original amount), respects an explicit AsOfDate rather than always using "now", and
/// stays tenant/building isolated. Same disclosed Docker-daemon limitation as every other
/// PostgresApiFactory-backed test in this project.
/// </summary>
public class ReceivablesAgeingReportDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly PostgresApiFactory _factory;

    public ReceivablesAgeingReportDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Seeded(Guid TenantId, Guid BuildingId, string AccessToken);

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

    private async Task<(Seeded Seeded, FlatId FlatId)> SeedTenantWithBuildingAsync(string slug)
    {
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using IServiceScope scope = _factory.Services.CreateScope();
        IBuildingRepository buildings = scope.ServiceProvider.GetRequiredService<IBuildingRepository>();
        IFlatRepository flats = scope.ServiceProvider.GetRequiredService<IFlatRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        Building building = Building.Create(tenantId, "Tower A", $"{slug}-A", null, clock.UtcNow);
        buildings.Add(building);

        Flat flat = Flat.Create(tenantId, building.Id, "A-101", 1, FlatType.Residential, clock.UtcNow);
        flats.Add(flat);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"owner-{slug}@example.com");

        return (new Seeded(tenantId, building.Id.Value, tokens.AccessToken), flat.Id);
    }

    private async Task SeedInvoiceAsync(
        Guid tenantId, BuildingId buildingId, FlatId flatId, string invoiceNumber, decimal totalAmount, DateOnly dueDate,
        decimal amountPaid = 0m)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IInvoiceRepository invoices = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        InvoiceLineInput line = new(null, "Service Charge", "Maintenance", "FixedAmount", totalAmount, null, 1, totalAmount, "Test line");
        (Invoice invoice, IReadOnlyList<InvoiceLine> lines) = Invoice.Issue(
            tenantId, buildingId, flatId, invoiceNumber, InvoiceSource.ServiceCharge,
            dueDate, dueDate, dueDate, dueDate, [line], LedgerPostingId.New(), clock.UtcNow);

        if (amountPaid > 0)
        {
            invoice.ApplyPayment(amountPaid);
        }

        invoices.Add(invoice);
        invoices.AddLines(lines);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<ReceivablesAgeingReportDto> GetAgeingAsync(string accessToken, DateOnly? asOfDate = null, Guid? buildingId = null)
    {
        using HttpClient client = _factory.CreateClient();
        string url = "/api/v1/reports/financial/receivables-ageing?"
            + (asOfDate is DateOnly d ? $"asOfDate={d:yyyy-MM-dd}&" : string.Empty)
            + (buildingId is Guid b ? $"buildingId={b}" : string.Empty);

        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Authorization = new("Bearer", accessToken);
        HttpResponseMessage response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ReceivablesAgeingReportDto? dto = await response.Content.ReadFromJsonAsync<ReceivablesAgeingReportDto>(JsonOptions);
        dto.Should().NotBeNull();
        return dto!;
    }

    [Fact]
    public async Task Ageing_Uses_Remaining_Balance_Not_Original_Amount_And_Buckets_By_AsOfDate()
    {
        (Seeded seeded, FlatId flatId) = await SeedTenantWithBuildingAsync("ageing-balance");
        DateOnly asOfDate = new(2026, 8, 15);

        // Overdue by 40 days as of asOfDate (2026-07-06 -> 40 days before 2026-08-15), partially paid —
        // must bucket on the REMAINING balance (1000 - 400 = 600), not the original 1000.
        await SeedInvoiceAsync(
            seeded.TenantId, new BuildingId(seeded.BuildingId), flatId, "INV-1", totalAmount: 1000m,
            dueDate: new DateOnly(2026, 7, 6), amountPaid: 400m);

        // Not yet due as of asOfDate -> Current bucket.
        await SeedInvoiceAsync(
            seeded.TenantId, new BuildingId(seeded.BuildingId), flatId, "INV-2", totalAmount: 250m,
            dueDate: new DateOnly(2026, 9, 1));

        ReceivablesAgeingReportDto ageing = await GetAgeingAsync(seeded.AccessToken, asOfDate);

        ageing.AsOfDate.Should().Be(asOfDate);
        AgeingBucketDto bucket31To60 = ageing.Buckets.Single(b => b.BucketLabel == "31-60 days");
        bucket31To60.InvoiceCount.Should().Be(1);
        bucket31To60.TotalBalance.Should().Be(600m, "bucketing must use remaining balance, not the invoice's original amount");

        AgeingBucketDto current = ageing.Buckets.Single(b => b.BucketLabel == "Current");
        current.InvoiceCount.Should().Be(1);
        current.TotalBalance.Should().Be(250m);

        ageing.GrandTotal.Should().Be(850m);
    }

    [Fact]
    public async Task Fully_Paid_Invoice_Is_Excluded_From_Every_Bucket()
    {
        (Seeded seeded, FlatId flatId) = await SeedTenantWithBuildingAsync("ageing-paid-excluded");
        DateOnly asOfDate = new(2026, 8, 15);

        await SeedInvoiceAsync(
            seeded.TenantId, new BuildingId(seeded.BuildingId), flatId, "INV-1", totalAmount: 500m,
            dueDate: new DateOnly(2026, 6, 1), amountPaid: 500m);

        ReceivablesAgeingReportDto ageing = await GetAgeingAsync(seeded.AccessToken, asOfDate);

        ageing.Buckets.Should().OnlyContain(b => b.InvoiceCount == 0 && b.TotalBalance == 0m);
        ageing.GrandTotal.Should().Be(0m);
    }

    [Fact]
    public async Task Omitted_AsOfDate_Defaults_To_The_Servers_Current_Business_Date()
    {
        (Seeded seeded, FlatId flatId) = await SeedTenantWithBuildingAsync("ageing-default-asof");

        // Due far in the past relative to "now" (whenever the test runs) -> lands in 91+ regardless.
        await SeedInvoiceAsync(
            seeded.TenantId, new BuildingId(seeded.BuildingId), flatId, "INV-1", totalAmount: 750m,
            dueDate: new DateOnly(2020, 1, 1));

        ReceivablesAgeingReportDto ageing = await GetAgeingAsync(seeded.AccessToken);

        ageing.AsOfDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        ageing.Buckets.Single(b => b.BucketLabel == "91+ days").TotalBalance.Should().Be(750m);
    }

    [Fact]
    public async Task Ageing_Does_Not_Leak_Across_Tenants()
    {
        (Seeded tenantOne, FlatId tenantOneFlatId) = await SeedTenantWithBuildingAsync("ageing-tenant-one");
        (Seeded tenantTwo, FlatId tenantTwoFlatId) = await SeedTenantWithBuildingAsync("ageing-tenant-two");
        DateOnly asOfDate = new(2026, 8, 15);

        await SeedInvoiceAsync(
            tenantOne.TenantId, new BuildingId(tenantOne.BuildingId), tenantOneFlatId, "T1-INV-1", totalAmount: 300m,
            dueDate: new DateOnly(2026, 8, 1));
        await SeedInvoiceAsync(
            tenantTwo.TenantId, new BuildingId(tenantTwo.BuildingId), tenantTwoFlatId, "T2-INV-1", totalAmount: 99_000m,
            dueDate: new DateOnly(2026, 8, 1));

        ReceivablesAgeingReportDto tenantOneAgeing = await GetAgeingAsync(tenantOne.AccessToken, asOfDate);

        tenantOneAgeing.GrandTotal.Should().Be(300m, "another tenant's receivables must never appear in this tenant's ageing report");
    }
}
