using AwesomeAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.GenerateSubscriptionInvoice;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Application.Features.Platform.Commands.RecordSubscriptionPayment;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof for <see cref="RecordSubscriptionPaymentCommand"/> against a real, ephemeral
/// PostgreSQL container (ADR-034 Task 14C) — same <see cref="ISender"/>-inside-a-DI-scope technique
/// <see cref="GenerateSubscriptionInvoiceDbTests"/> already establishes. Needs a Docker daemon — see
/// <see cref="PostgresApiFactory"/>.
/// </summary>
public class RecordSubscriptionPaymentDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public RecordSubscriptionPaymentDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);
    private static readonly DateOnly BillingPeriodStart = new(2026, 3, 1);
    private static readonly DateOnly PaymentDate = new(2026, 3, 5);

    private static async Task<SubscriptionPackageVersion> CreateAssignablePackageVersionAsync(IServiceScope scope)
    {
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{Guid.NewGuid():N}", "Professional", null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, EffectiveFrom, null,
            monthlyPrice: 8000m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: 84000m, currency: "BDT");
        version.Activate();
        package.Activate();
        package.SetCurrentVersion(version.Id);

        packages.Add(package);
        versions.Add(version);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return version;
    }

    private static async Task<Guid> ProvisionOrganizationAsync(IServiceScope scope, Guid packageVersionId, string suffix)
    {
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        ProvisionOrganizationResult result = await sender.Send(
            new ProvisionOrganizationWithAdminCommand(
                Name: "Task 14C Integration Org",
                Code: $"T14C-{suffix.ToUpperInvariant()}",
                Slug: $"task14c-{suffix}",
                AdministratorFullName: "Test Admin",
                AdministratorEmail: $"admin@task14c-{suffix}.test",
                AdministratorPassword: "Correct-Horse-Battery-9",
                EnabledModuleKeys: ["billing", "payments"],
                SubscriptionPackageVersionId: packageVersionId,
                BillingCycle: BillingCycle.Monthly,
                AutoRenew: false),
            CancellationToken.None);

        return result.TenantId;
    }

    private static async Task<(Guid TenantId, Guid InvoiceId, decimal TotalAmount)> ProvisionOrganizationWithInvoiceAsync(
        PostgresApiFactory factory, string suffix)
    {
        using IServiceScope setupScope = factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreateAssignablePackageVersionAsync(setupScope);
        Guid tenantId = await ProvisionOrganizationAsync(setupScope, version.Id.Value, suffix);

        using IServiceScope invoiceScope = factory.Services.CreateScope();
        Guid invoiceId = await invoiceScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new GenerateSubscriptionInvoiceCommand(tenantId, BillingPeriodStart), CancellationToken.None);

        using IServiceScope readScope = factory.Services.CreateScope();
        SubscriptionInvoice invoice = (await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionInvoiceRepository>()
            .GetByIdAsync(new SubscriptionInvoiceId(invoiceId), CancellationToken.None))!;

        return (tenantId, invoiceId, invoice.TotalAmount);
    }

    [Fact]
    public async Task Records_A_Full_Payment_And_Transitions_The_Invoice_To_Paid()
    {
        (Guid tenantId, Guid invoiceId, decimal totalAmount) = await ProvisionOrganizationWithInvoiceAsync(_factory, "full-payment");

        using IServiceScope scope = _factory.Services.CreateScope();
        Guid paymentId = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RecordSubscriptionPaymentCommand(tenantId, invoiceId, totalAmount, "BDT", PaymentDate, "BANK-REF-FULL", "Wire transfer"),
            CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        SubscriptionInvoice invoice = (await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionInvoiceRepository>()
            .GetByIdAsync(new SubscriptionInvoiceId(invoiceId), CancellationToken.None))!;
        SubscriptionPayment payment = (await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionPaymentRepository>()
            .GetByIdAsync(new SubscriptionPaymentId(paymentId), CancellationToken.None))!;

        payment.Should().NotBeNull();
        payment.TenantId.Should().Be(tenantId);
        payment.SubscriptionInvoiceId.Should().Be(invoice.Id);
        payment.Amount.Should().Be(totalAmount);

        invoice.OutstandingAmount.Should().Be(0m);
        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Paid);
    }

    [Fact]
    public async Task Records_A_Partial_Payment_And_Leaves_The_Invoice_Issued()
    {
        (Guid tenantId, Guid invoiceId, decimal totalAmount) = await ProvisionOrganizationWithInvoiceAsync(_factory, "partial-payment");

        using IServiceScope scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RecordSubscriptionPaymentCommand(tenantId, invoiceId, 3000m, "BDT", PaymentDate, "BANK-REF-PARTIAL", null),
            CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        SubscriptionInvoice invoice = (await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionInvoiceRepository>()
            .GetByIdAsync(new SubscriptionInvoiceId(invoiceId), CancellationToken.None))!;

        invoice.OutstandingAmount.Should().Be(totalAmount - 3000m);
        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Issued);
    }

    [Fact]
    public async Task Rejects_A_Payment_Exceeding_The_Outstanding_Amount()
    {
        (Guid tenantId, Guid invoiceId, decimal totalAmount) = await ProvisionOrganizationWithInvoiceAsync(_factory, "overpayment");

        using IServiceScope scope = _factory.Services.CreateScope();
        Func<Task> act = async () => await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RecordSubscriptionPaymentCommand(tenantId, invoiceId, totalAmount + 0.01m, "BDT", PaymentDate, "BANK-REF-OVER", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionInvoicePaymentExceedsOutstandingException>();

        using IServiceScope readScope = _factory.Services.CreateScope();
        SubscriptionInvoice invoice = (await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionInvoiceRepository>()
            .GetByIdAsync(new SubscriptionInvoiceId(invoiceId), CancellationToken.None))!;

        invoice.OutstandingAmount.Should().Be(totalAmount);
        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Issued);
    }

    [Fact]
    public async Task Rejects_A_Currency_Mismatched_Payment()
    {
        (Guid tenantId, Guid invoiceId, decimal totalAmount) = await ProvisionOrganizationWithInvoiceAsync(_factory, "currency-mismatch");

        using IServiceScope scope = _factory.Services.CreateScope();
        Func<Task> act = async () => await scope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RecordSubscriptionPaymentCommand(tenantId, invoiceId, 1000m, "USD", PaymentDate, "BANK-REF-USD", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionInvoiceCurrencyMismatchException>();

        using IServiceScope readScope = _factory.Services.CreateScope();
        SubscriptionInvoice invoice = (await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionInvoiceRepository>()
            .GetByIdAsync(new SubscriptionInvoiceId(invoiceId), CancellationToken.None))!;

        invoice.OutstandingAmount.Should().Be(totalAmount);
        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Issued);
    }

    [Fact]
    public async Task Rejects_Payment_Against_An_Already_Paid_Invoice()
    {
        (Guid tenantId, Guid invoiceId, decimal totalAmount) = await ProvisionOrganizationWithInvoiceAsync(_factory, "already-paid");

        using IServiceScope firstScope = _factory.Services.CreateScope();
        await firstScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RecordSubscriptionPaymentCommand(tenantId, invoiceId, totalAmount, "BDT", PaymentDate, "BANK-REF-FIRST", null),
            CancellationToken.None);

        using IServiceScope secondScope = _factory.Services.CreateScope();
        Func<Task> act = async () => await secondScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RecordSubscriptionPaymentCommand(tenantId, invoiceId, 100m, "BDT", PaymentDate, "BANK-REF-SECOND", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionInvoiceNotPayableException>();
    }

    [Fact]
    public async Task Rejects_A_Second_Payment_Using_The_Same_Reference_Number()
    {
        (Guid tenantId, Guid invoiceId, decimal totalAmount) = await ProvisionOrganizationWithInvoiceAsync(_factory, "duplicate-reference");

        using IServiceScope firstScope = _factory.Services.CreateScope();
        await firstScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RecordSubscriptionPaymentCommand(tenantId, invoiceId, 2000m, "BDT", PaymentDate, "DUP-REF", null),
            CancellationToken.None);

        using IServiceScope secondScope = _factory.Services.CreateScope();
        Func<Task> act = async () => await secondScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new RecordSubscriptionPaymentCommand(tenantId, invoiceId, 2000m, "BDT", PaymentDate, "DUP-REF", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();

        using IServiceScope readScope = _factory.Services.CreateScope();
        SubscriptionInvoice invoice = (await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionInvoiceRepository>()
            .GetByIdAsync(new SubscriptionInvoiceId(invoiceId), CancellationToken.None))!;

        // Only the first payment's amount was ever applied — the rejected duplicate left no trace,
        // proving payment + invoice update stayed atomic (ADR-034 Task 14C §"Persistence / Transaction").
        invoice.OutstandingAmount.Should().Be(totalAmount - 2000m);
    }
}
