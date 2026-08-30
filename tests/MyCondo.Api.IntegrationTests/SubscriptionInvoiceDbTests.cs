using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory) for the
/// SubscriptionInvoice foundation (ADR-034 Task 14A) — no RLS on the
/// <c>platform.subscription_invoices</c>/<c>platform.subscription_invoice_lines</c> tables
/// (platform-schema, not tenant data), same reasoning as OrganizationSubscriptionDbTests. These need a
/// Docker daemon and were NOT executed in the environment they were authored in — see
/// PostgresApiFactory's doc comment. Run wherever Docker is available before trusting them.
/// </summary>
public class SubscriptionInvoiceDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public SubscriptionInvoiceDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly DateOnly PeriodStart = new(2026, 3, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 3, 31);
    private static readonly DateTimeOffset IssuedAt = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    private static SubscriptionInvoiceLineInput OneLine() => new(
        SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, 8000m, 0m, 8000m,
        "Professional (Monthly)");

    [Fact]
    public async Task SubscriptionInvoice_Persists_And_Round_Trips_With_Lines()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionInvoiceRepository invoices = scope.ServiceProvider.GetRequiredService<ISubscriptionInvoiceRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        OrganizationSubscriptionId subscriptionId = OrganizationSubscriptionId.New();

        (SubscriptionInvoice invoice, IReadOnlyList<SubscriptionInvoiceLine> lines) = SubscriptionInvoice.Issue(
            tenantId, subscriptionId, $"SUBINV-{Guid.NewGuid():N}", PeriodStart, PeriodEnd, PeriodStart, PeriodEnd,
            "BDT", [OneLine()], IssuedAt);

        invoices.Add(invoice);
        scope.ServiceProvider.GetRequiredService<MyCondoDbContext>().Set<SubscriptionInvoiceLine>().AddRange(lines);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        SubscriptionInvoice? reloaded = await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionInvoiceRepository>()
            .GetByIdAsync(invoice.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.TenantId.Should().Be(tenantId);
        reloaded.OrganizationSubscriptionId.Should().Be(subscriptionId);
        reloaded.BillingPeriodStart.Should().Be(PeriodStart);
        reloaded.BillingPeriodEnd.Should().Be(PeriodEnd);
        reloaded.Currency.Should().Be("BDT");
        reloaded.TotalAmount.Should().Be(8000m);
        reloaded.OutstandingAmount.Should().Be(8000m);
        reloaded.Status.Should().Be(SubscriptionInvoiceStatus.Issued);
    }

    [Fact]
    public async Task SubscriptionInvoice_Rejects_A_Second_Invoice_For_The_Same_Subscription_And_Period()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionInvoiceRepository invoices = scope.ServiceProvider.GetRequiredService<ISubscriptionInvoiceRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        OrganizationSubscriptionId subscriptionId = OrganizationSubscriptionId.New();

        (SubscriptionInvoice first, _) = SubscriptionInvoice.Issue(
            tenantId, subscriptionId, $"SUBINV-{Guid.NewGuid():N}", PeriodStart, PeriodEnd, PeriodStart, PeriodEnd,
            "BDT", [OneLine()], IssuedAt);
        invoices.Add(first);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope secondScope = _factory.Services.CreateScope();
        ISubscriptionInvoiceRepository secondInvoices = secondScope.ServiceProvider.GetRequiredService<ISubscriptionInvoiceRepository>();
        IUnitOfWork secondUnitOfWork = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        (SubscriptionInvoice second, _) = SubscriptionInvoice.Issue(
            tenantId, subscriptionId, $"SUBINV-{Guid.NewGuid():N}", PeriodStart, PeriodEnd, PeriodStart, PeriodEnd,
            "BDT", [OneLine()], IssuedAt);
        secondInvoices.Add(second);

        Func<Task> act = () => secondUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task SubscriptionInvoice_Rejects_A_Duplicate_InvoiceNumber_For_The_Same_Tenant()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionInvoiceRepository invoices = scope.ServiceProvider.GetRequiredService<ISubscriptionInvoiceRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        string invoiceNumber = $"SUBINV-{Guid.NewGuid():N}";

        (SubscriptionInvoice first, _) = SubscriptionInvoice.Issue(
            tenantId, OrganizationSubscriptionId.New(), invoiceNumber, PeriodStart, PeriodEnd, PeriodStart, PeriodEnd,
            "BDT", [OneLine()], IssuedAt);
        invoices.Add(first);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope secondScope = _factory.Services.CreateScope();
        ISubscriptionInvoiceRepository secondInvoices = secondScope.ServiceProvider.GetRequiredService<ISubscriptionInvoiceRepository>();
        IUnitOfWork secondUnitOfWork = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Different subscription/period, same tenant + invoice number.
        (SubscriptionInvoice second, _) = SubscriptionInvoice.Issue(
            tenantId, OrganizationSubscriptionId.New(), invoiceNumber, PeriodStart.AddMonths(1), PeriodEnd.AddMonths(1),
            PeriodStart.AddMonths(1), PeriodEnd.AddMonths(1), "BDT", [OneLine()], IssuedAt);
        secondInvoices.Add(second);

        Func<Task> act = () => secondUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }
}
