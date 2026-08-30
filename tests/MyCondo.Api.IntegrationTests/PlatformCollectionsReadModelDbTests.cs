using AwesomeAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Application.Features.Platform.Commands.RecordSubscriptionPayment;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetOutstandingDues;
using MyCondo.Application.Features.Platform.Queries.GetSubscriptionInvoiceById;
using MyCondo.Application.Features.Platform.Queries.GetSubscriptionInvoicePayments;
using MyCondo.Application.Features.Platform.Queries.ListSubscriptionInvoices;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof for the Task 14D Platform billing read model (subscription-invoice/outstanding-dues/
/// collections queries) against a real, ephemeral PostgreSQL container — same
/// <see cref="ISender"/>-inside-a-DI-scope technique <see cref="GenerateSubscriptionInvoiceDbTests"/>
/// already establishes. Invoices are issued directly via <see cref="SubscriptionInvoice.Issue"/> +
/// repository <c>Add</c> (the same technique <see cref="SubscriptionInvoiceDbTests"/> uses) rather than
/// through <c>GenerateSubscriptionInvoiceCommand</c>, so each test can pin an exact <c>DueDate</c>
/// relative to the real wall clock — the command handler always derives <c>DueDate</c> from "now",
/// which cannot produce a controllable overdue/not-yet-due fixture. Needs a Docker daemon — see
/// <see cref="PostgresApiFactory"/>.
/// </summary>
public class PlatformCollectionsReadModelDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public PlatformCollectionsReadModelDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly DateOnly EffectiveFrom = new(2020, 1, 1);

    private static async Task<Guid> ProvisionOrganizationAsync(PostgresApiFactory factory, string suffix)
    {
        using IServiceScope packageScope = factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = packageScope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = packageScope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork packageUnitOfWork = packageScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{Guid.NewGuid():N}", "Professional", null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, EffectiveFrom, null,
            monthlyPrice: 8000m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: 84000m, currency: "BDT");
        version.Activate();
        package.Activate();
        package.SetCurrentVersion(version.Id);

        packages.Add(package);
        versions.Add(version);
        await packageUnitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope orgScope = factory.Services.CreateScope();
        ProvisionOrganizationResult result = await orgScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new ProvisionOrganizationWithAdminCommand(
                Name: $"Task 14D Collections Org {suffix}",
                Code: $"T14D-{suffix.ToUpperInvariant()}",
                Slug: $"task14d-{suffix.ToLowerInvariant()}",
                AdministratorFullName: "Test Admin",
                AdministratorEmail: $"admin@task14d-{suffix.ToLowerInvariant()}.test",
                AdministratorPassword: "Correct-Horse-Battery-9",
                EnabledModuleKeys: ["billing", "payments"],
                SubscriptionPackageVersionId: version.Id.Value,
                BillingCycle: BillingCycle.Monthly,
                AutoRenew: false),
            CancellationToken.None);

        return result.TenantId;
    }

    private static async Task<SubscriptionInvoice> IssueInvoiceDirectAsync(
        PostgresApiFactory factory, Guid tenantId, DateOnly dueDate, decimal amount = 8000m, string currency = "BDT")
    {
        DateOnly periodStart = dueDate.AddDays(-30);
        DateTimeOffset issuedAt = new(periodStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        using IServiceScope scope = factory.Services.CreateScope();
        ISubscriptionInvoiceRepository invoices = scope.ServiceProvider.GetRequiredService<ISubscriptionInvoiceRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        (SubscriptionInvoice invoice, IReadOnlyList<SubscriptionInvoiceLine> lines) = SubscriptionInvoice.Issue(
            tenantId, OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}", periodStart, dueDate, periodStart,
            dueDate, currency,
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, amount, 0m, amount,
                "Professional (Monthly)")],
            issuedAt);

        invoices.Add(invoice);
        scope.ServiceProvider.GetRequiredService<MyCondoDbContext>().Set<SubscriptionInvoiceLine>().AddRange(lines);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return invoice;
    }

    private static async Task VoidInvoiceDirectAsync(PostgresApiFactory factory, SubscriptionInvoiceId invoiceId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        ISubscriptionInvoiceRepository invoices = scope.ServiceProvider.GetRequiredService<ISubscriptionInvoiceRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionInvoice invoice = (await invoices.GetByIdAsync(invoiceId, CancellationToken.None))!;
        invoice.Void("Billed against the wrong subscription", DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task CancelInvoiceDirectAsync(PostgresApiFactory factory, SubscriptionInvoiceId invoiceId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        ISubscriptionInvoiceRepository invoices = scope.ServiceProvider.GetRequiredService<ISubscriptionInvoiceRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionInvoice invoice = (await invoices.GetByIdAsync(invoiceId, CancellationToken.None))!;
        invoice.Cancel("Commercial waiver", DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private static ISender Sender(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ISender>();

    [Fact]
    public async Task An_Unpaid_Issued_Invoice_Appears_As_Outstanding()
    {
        Guid tenantId = await ProvisionOrganizationAsync(_factory, "unpaid");
        SubscriptionInvoice invoice = await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));

        using IServiceScope scope = _factory.Services.CreateScope();
        PagedResult<PlatformOrganizationOutstandingDto> result =
            await Sender(scope).Send(new GetOutstandingDuesQuery(OrganizationId: tenantId), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].OutstandingAmount.Should().Be(invoice.TotalAmount);
        result.Items[0].MaxDaysOverdue.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task A_Partially_Paid_Invoice_Reports_Only_The_Remaining_Outstanding_Amount()
    {
        Guid tenantId = await ProvisionOrganizationAsync(_factory, "partial");
        SubscriptionInvoice invoice = await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

        using (IServiceScope paymentScope = _factory.Services.CreateScope())
        {
            await Sender(paymentScope).Send(
                new RecordSubscriptionPaymentCommand(
                    tenantId, invoice.Id.Value, 3000m, "BDT", DateOnly.FromDateTime(DateTime.UtcNow), "BANK-REF-PARTIAL", null),
                CancellationToken.None);
        }

        using IServiceScope readScope = _factory.Services.CreateScope();
        PlatformSubscriptionInvoiceDetailDto detail = await Sender(readScope).Send(
            new GetSubscriptionInvoiceByIdQuery(invoice.Id.Value), CancellationToken.None);

        detail.OutstandingAmount.Should().Be(invoice.TotalAmount - 3000m);
        detail.Status.Should().Be(nameof(SubscriptionInvoiceStatus.Issued));

        PagedResult<PlatformOrganizationOutstandingDto> outstanding = await Sender(readScope).Send(
            new GetOutstandingDuesQuery(OrganizationId: tenantId), CancellationToken.None);
        outstanding.Items.Should().ContainSingle();
        outstanding.Items[0].OutstandingAmount.Should().Be(invoice.TotalAmount - 3000m);
    }

    [Fact]
    public async Task A_Fully_Paid_Invoice_Is_Excluded_From_Collectible_Outstanding()
    {
        Guid tenantId = await ProvisionOrganizationAsync(_factory, "fullypaid");
        SubscriptionInvoice invoice = await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

        using (IServiceScope paymentScope = _factory.Services.CreateScope())
        {
            await Sender(paymentScope).Send(
                new RecordSubscriptionPaymentCommand(
                    tenantId, invoice.Id.Value, invoice.TotalAmount, "BDT", DateOnly.FromDateTime(DateTime.UtcNow), "BANK-REF-FULL", null),
                CancellationToken.None);
        }

        using IServiceScope readScope = _factory.Services.CreateScope();
        PagedResult<PlatformOrganizationOutstandingDto> outstanding = await Sender(readScope).Send(
            new GetOutstandingDuesQuery(OrganizationId: tenantId), CancellationToken.None);

        outstanding.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task A_Void_Invoice_Is_Excluded_From_Collectible_Outstanding()
    {
        Guid tenantId = await ProvisionOrganizationAsync(_factory, "voided");
        SubscriptionInvoice invoice = await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));
        await VoidInvoiceDirectAsync(_factory, invoice.Id);

        using IServiceScope scope = _factory.Services.CreateScope();
        PagedResult<PlatformOrganizationOutstandingDto> outstanding =
            await Sender(scope).Send(new GetOutstandingDuesQuery(OrganizationId: tenantId), CancellationToken.None);

        outstanding.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task A_Canceled_Invoice_Is_Excluded_From_Collectible_Outstanding()
    {
        Guid tenantId = await ProvisionOrganizationAsync(_factory, "canceled");
        SubscriptionInvoice invoice = await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));
        await CancelInvoiceDirectAsync(_factory, invoice.Id);

        using IServiceScope scope = _factory.Services.CreateScope();
        PagedResult<PlatformOrganizationOutstandingDto> outstanding =
            await Sender(scope).Send(new GetOutstandingDuesQuery(OrganizationId: tenantId), CancellationToken.None);

        outstanding.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task DaysOverdue_Is_Computed_From_DueDate_Not_BillingPeriodEnd()
    {
        Guid tenantId = await ProvisionOrganizationAsync(_factory, "overdue-calc");
        DateOnly dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-12));
        SubscriptionInvoice invoice = await IssueInvoiceDirectAsync(_factory, tenantId, dueDate);

        using IServiceScope scope = _factory.Services.CreateScope();
        PlatformSubscriptionInvoiceDetailDto detail =
            await Sender(scope).Send(new GetSubscriptionInvoiceByIdQuery(invoice.Id.Value), CancellationToken.None);

        detail.DueDate.Should().Be(dueDate);
        detail.DaysOverdue.Should().NotBeNull();
        // Dhaka is UTC+6, so the handler's local "today" is never behind DateTime.UtcNow's date, only
        // possibly a day ahead of it near the UTC day boundary — tolerate that, nothing wider.
        detail.DaysOverdue!.Value.Should().BeInRange(12, 13);
    }

    [Fact]
    public async Task A_Not_Yet_Due_Invoice_Has_No_DaysOverdue_And_Is_Excluded_By_OverdueOnly()
    {
        Guid tenantId = await ProvisionOrganizationAsync(_factory, "notdue");
        SubscriptionInvoice invoice = await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)));

        using IServiceScope scope = _factory.Services.CreateScope();
        PlatformSubscriptionInvoiceDetailDto detail =
            await Sender(scope).Send(new GetSubscriptionInvoiceByIdQuery(invoice.Id.Value), CancellationToken.None);
        detail.DaysOverdue.Should().BeNull();

        PagedResult<PlatformSubscriptionInvoiceListItemDto> overdueOnly = await Sender(scope).Send(
            new ListSubscriptionInvoicesQuery(OrganizationId: tenantId, OverdueOnly: true), CancellationToken.None);
        overdueOnly.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Organization_Outstanding_Total_Equals_The_Sum_Of_Eligible_Persisted_OutstandingAmounts()
    {
        Guid tenantId = await ProvisionOrganizationAsync(_factory, "sumcheck");
        await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), amount: 5000m);
        await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), amount: 3000m);

        using IServiceScope scope = _factory.Services.CreateScope();
        PagedResult<PlatformOrganizationOutstandingDto> outstanding =
            await Sender(scope).Send(new GetOutstandingDuesQuery(OrganizationId: tenantId), CancellationToken.None);

        outstanding.Items.Should().ContainSingle();
        outstanding.Items[0].OutstandingAmount.Should().Be(8000m);
        outstanding.Items[0].OutstandingInvoiceCount.Should().Be(2);
    }

    [Fact]
    public async Task Organizations_And_Their_Invoices_Remain_Correctly_Separated()
    {
        Guid tenantA = await ProvisionOrganizationAsync(_factory, "sepA");
        Guid tenantB = await ProvisionOrganizationAsync(_factory, "sepB");
        await IssueInvoiceDirectAsync(_factory, tenantA, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), amount: 4000m);
        await IssueInvoiceDirectAsync(_factory, tenantB, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), amount: 9000m);

        using IServiceScope scope = _factory.Services.CreateScope();
        PagedResult<PlatformOrganizationOutstandingDto> outstandingA =
            await Sender(scope).Send(new GetOutstandingDuesQuery(OrganizationId: tenantA), CancellationToken.None);
        PagedResult<PlatformOrganizationOutstandingDto> outstandingB =
            await Sender(scope).Send(new GetOutstandingDuesQuery(OrganizationId: tenantB), CancellationToken.None);

        outstandingA.Items.Should().ContainSingle();
        outstandingA.Items[0].OutstandingAmount.Should().Be(4000m);
        outstandingB.Items.Should().ContainSingle();
        outstandingB.Items[0].OutstandingAmount.Should().Be(9000m);

        PagedResult<PlatformSubscriptionInvoiceListItemDto> invoicesForA = await Sender(scope).Send(
            new ListSubscriptionInvoicesQuery(OrganizationId: tenantA), CancellationToken.None);
        invoicesForA.Items.Should().OnlyContain(i => i.TenantId == tenantA);
    }

    [Fact]
    public async Task Currency_Is_Preserved_And_Unlike_Currencies_Are_Not_Summed_Together()
    {
        Guid tenantId = await ProvisionOrganizationAsync(_factory, "multicur");
        await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), amount: 5000m, currency: "BDT");
        await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), amount: 100m, currency: "USD");

        using IServiceScope scope = _factory.Services.CreateScope();
        PagedResult<PlatformOrganizationOutstandingDto> outstanding =
            await Sender(scope).Send(new GetOutstandingDuesQuery(OrganizationId: tenantId), CancellationToken.None);

        outstanding.Items.Should().HaveCount(2);
        outstanding.Items.Should().Contain(x => x.Currency == "BDT" && x.OutstandingAmount == 5000m);
        outstanding.Items.Should().Contain(x => x.Currency == "USD" && x.OutstandingAmount == 100m);
    }

    [Fact]
    public async Task Invoice_Payment_History_Is_Exposed_Through_The_Dedicated_Payments_Query()
    {
        Guid tenantId = await ProvisionOrganizationAsync(_factory, "payhist");
        SubscriptionInvoice invoice = await IssueInvoiceDirectAsync(_factory, tenantId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

        using (IServiceScope paymentScope = _factory.Services.CreateScope())
        {
            await Sender(paymentScope).Send(
                new RecordSubscriptionPaymentCommand(
                    tenantId, invoice.Id.Value, 1500m, "BDT", DateOnly.FromDateTime(DateTime.UtcNow), "BANK-REF-HIST", "First installment"),
                CancellationToken.None);
        }

        using IServiceScope readScope = _factory.Services.CreateScope();
        IReadOnlyList<PlatformSubscriptionPaymentDto> payments = await Sender(readScope).Send(
            new GetSubscriptionInvoicePaymentsQuery(invoice.Id.Value), CancellationToken.None);

        payments.Should().ContainSingle();
        payments[0].Amount.Should().Be(1500m);
        payments[0].ReferenceNumber.Should().Be("BANK-REF-HIST");
    }

    [Fact]
    public async Task GetSubscriptionInvoiceById_Throws_NotFound_For_An_Unknown_Invoice()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        Func<Task> act = async () => await Sender(scope).Send(
            new GetSubscriptionInvoiceByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
