using AwesomeAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.GenerateSubscriptionInvoice;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof for <see cref="GenerateSubscriptionInvoiceCommand"/> against a real, ephemeral
/// PostgreSQL container (ADR-034 Task 14B) — same <see cref="ISender"/>-inside-a-DI-scope technique
/// <see cref="ChangeOrganizationSubscriptionDbTests"/> already establishes. Needs a Docker daemon — see
/// <see cref="PostgresApiFactory"/>.
/// </summary>
public class GenerateSubscriptionInvoiceDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public GenerateSubscriptionInvoiceDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);
    private static readonly DateOnly BillingPeriodStart = new(2026, 3, 1);

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
                Name: "Task 14B Integration Org",
                Code: $"T14B-{suffix.ToUpperInvariant()}",
                Slug: $"task14b-{suffix}",
                AdministratorFullName: "Test Admin",
                AdministratorEmail: $"admin@task14b-{suffix}.test",
                AdministratorPassword: "Correct-Horse-Battery-9",
                EnabledModuleKeys: ["billing", "payments"],
                SubscriptionPackageVersionId: packageVersionId,
                BillingCycle: BillingCycle.Monthly,
                AutoRenew: false),
            CancellationToken.None);

        return result.TenantId;
    }

    [Fact]
    public async Task Generates_An_Invoice_From_The_Subscription_Commercial_Snapshot()
    {
        using IServiceScope setupScope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreateAssignablePackageVersionAsync(setupScope);
        Guid tenantId = await ProvisionOrganizationAsync(setupScope, version.Id.Value, "generate-success");

        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Guid invoiceId = await sender.Send(
            new GenerateSubscriptionInvoiceCommand(tenantId, BillingPeriodStart), CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription subscription = (await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetCurrentForTenantAsync(tenantId, CancellationToken.None))!;
        SubscriptionInvoice? invoice = await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionInvoiceRepository>()
            .GetByIdAsync(new SubscriptionInvoiceId(invoiceId), CancellationToken.None);

        invoice.Should().NotBeNull();
        invoice!.TenantId.Should().Be(tenantId);
        invoice.OrganizationSubscriptionId.Should().Be(subscription.Id);
        invoice.BillingPeriodStart.Should().Be(BillingPeriodStart);
        invoice.BillingPeriodEnd.Should().Be(new DateOnly(2026, 3, 31));
        // DueDate is BillingPeriodEnd clamped forward to IssueDate (never before it) — BillingPeriodStart
        // is a fixed 2026-03-01 fixture date, so whenever "now" runs after 2026-03-31 (any real clock
        // running this suite today) DueDate clamps to IssueDate instead.
        invoice.DueDate.Should().BeOnOrAfter(invoice.IssueDate);
        invoice.DueDate.Should().Be(invoice.BillingPeriodEnd > invoice.IssueDate ? invoice.BillingPeriodEnd : invoice.IssueDate);
        invoice.Currency.Should().Be(subscription.Currency);
        invoice.TotalAmount.Should().Be(subscription.EffectivePrice);
        invoice.OutstandingAmount.Should().Be(invoice.TotalAmount);
        invoice.Status.Should().Be(SubscriptionInvoiceStatus.Issued);
    }

    [Fact]
    public async Task Rejects_A_Second_Generation_For_The_Same_Subscription_And_Period()
    {
        using IServiceScope setupScope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreateAssignablePackageVersionAsync(setupScope);
        Guid tenantId = await ProvisionOrganizationAsync(setupScope, version.Id.Value, "generate-duplicate");

        using IServiceScope firstScope = _factory.Services.CreateScope();
        await firstScope.ServiceProvider.GetRequiredService<ISender>().Send(
            new GenerateSubscriptionInvoiceCommand(tenantId, BillingPeriodStart), CancellationToken.None);

        using IServiceScope secondScope = _factory.Services.CreateScope();
        ISender secondSender = secondScope.ServiceProvider.GetRequiredService<ISender>();

        Func<Task> act = async () => await secondSender.Send(
            new GenerateSubscriptionInvoiceCommand(tenantId, BillingPeriodStart), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription subscription = (await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetCurrentForTenantAsync(tenantId, CancellationToken.None))!;
        IReadOnlyList<SubscriptionInvoice> invoices = await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionInvoiceRepository>()
            .GetForOrganizationSubscriptionAsync(subscription.Id, CancellationToken.None);

        invoices.Should().HaveCount(1);
    }
}
