using AwesomeAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Features.Platform.Commands.ApplyOrganizationSubscriptionBillingDecision;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Application.Features.Platform.Services.BillingLifecycleDecision;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof for <see cref="ApplyOrganizationSubscriptionBillingDecisionCommand"/> (ADR-034 Task
/// 14F) against a real, ephemeral PostgreSQL container — same <see cref="ISender"/>-inside-a-DI-scope
/// technique <see cref="RecordSubscriptionPaymentDbTests"/> already establishes. Overdue invoices are
/// issued directly through <see cref="ISubscriptionInvoiceRepository"/> (not
/// <c>GenerateSubscriptionInvoiceCommand</c>, whose due date is clamped forward to "today" and can never
/// be overdue) so the Task 14E decision matrix actually has an overdue fact to evaluate. Needs a Docker
/// daemon — see <see cref="PostgresApiFactory"/>.
/// </summary>
public class ApplyOrganizationSubscriptionBillingDecisionDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ApplyOrganizationSubscriptionBillingDecisionDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

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
                Name: "Task 14F Integration Org",
                Code: $"T14F-{suffix.ToUpperInvariant()}",
                Slug: $"task14f-{suffix}",
                AdministratorFullName: "Test Admin",
                AdministratorEmail: $"admin@task14f-{suffix}.test",
                AdministratorPassword: "Correct-Horse-Battery-9",
                EnabledModuleKeys: ["billing", "payments"],
                SubscriptionPackageVersionId: packageVersionId,
                BillingCycle: BillingCycle.Monthly,
                AutoRenew: false),
            CancellationToken.None);

        return result.TenantId;
    }

    /// <summary>Issues a collectible invoice directly through the repository, overdue by
    /// <paramref name="daysOverdue"/> relative to the real UTC clock's Dhaka business date — the same
    /// persisted fact <see cref="SubscriptionInvoiceOverdueCalculator"/> reads, deliberately bypassing
    /// <c>GenerateSubscriptionInvoiceCommand</c>'s issue-date clamp.</summary>
    private static async Task IssueOverdueInvoiceAsync(
        IServiceScope scope, Guid tenantId, OrganizationSubscriptionId subscriptionId, decimal amount, string currency, int daysOverdue)
    {
        ISubscriptionInvoiceRepository invoices = scope.ServiceProvider.GetRequiredService<ISubscriptionInvoiceRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

        DateOnly today = DateOnly.FromDateTime(MyCondo.Application.Common.DhakaTimeZone.ToLocal(clock.UtcNow).DateTime);
        DateOnly dueDate = today.AddDays(-daysOverdue);
        DateOnly billingPeriodStart = dueDate.AddDays(-30);

        (SubscriptionInvoice invoice, IReadOnlyList<SubscriptionInvoiceLine> lines) = SubscriptionInvoice.Issue(
            tenantId, subscriptionId, $"SUBINV-{Guid.NewGuid():N}",
            billingPeriodStart, dueDate, billingPeriodStart, dueDate, currency,
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, amount, 0m, amount, "Professional (Monthly)")],
            clock.UtcNow);

        invoices.Add(invoice);
        invoices.AddLines(lines);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Active_Organization_With_An_Overdue_Invoice_Persists_The_PastDue_Transition()
    {
        using IServiceScope setupScope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreateAssignablePackageVersionAsync(setupScope);
        Guid tenantId = await ProvisionOrganizationAsync(setupScope, version.Id.Value, "pastdue");

        using IServiceScope subscriptionScope = _factory.Services.CreateScope();
        OrganizationSubscription subscription = (await subscriptionScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetCurrentForTenantAsync(tenantId, CancellationToken.None))!;

        using IServiceScope invoiceScope = _factory.Services.CreateScope();
        await IssueOverdueInvoiceAsync(invoiceScope, tenantId, subscription.Id, subscription.EffectivePrice, subscription.Currency, daysOverdue: 5);

        using IServiceScope applyScope = _factory.Services.CreateScope();
        ApplyOrganizationSubscriptionBillingDecisionResult result = await applyScope.ServiceProvider
            .GetRequiredService<ISender>()
            .Send(new ApplyOrganizationSubscriptionBillingDecisionCommand(tenantId), CancellationToken.None);

        result.TransitionApplied.Should().BeTrue();
        result.Recommendation.Should().Be(BillingLifecycleRecommendedAction.MarkPastDue);
        result.PreviousStatus.Should().Be(OrganizationSubscriptionStatus.Active);
        result.ResultingStatus.Should().Be(OrganizationSubscriptionStatus.PastDue);

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription reloaded = (await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetByIdAsync(subscription.Id, CancellationToken.None))!;
        reloaded.Status.Should().Be(OrganizationSubscriptionStatus.PastDue);
    }

    [Fact]
    public async Task Reapplying_Immediately_Within_The_Grace_Window_Produces_NoAction_Rather_Than_Forcing_Restrict()
    {
        using IServiceScope setupScope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreateAssignablePackageVersionAsync(setupScope);
        Guid tenantId = await ProvisionOrganizationAsync(setupScope, version.Id.Value, "onestep");

        using IServiceScope subscriptionScope = _factory.Services.CreateScope();
        OrganizationSubscription subscription = (await subscriptionScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetCurrentForTenantAsync(tenantId, CancellationToken.None))!;

        // 5 days overdue: past the Active->PastDue threshold, but nowhere near the PastDue->Restricted
        // 30-day threshold — proves a single invocation never chains Active all the way to Restricted.
        using IServiceScope invoiceScope = _factory.Services.CreateScope();
        await IssueOverdueInvoiceAsync(invoiceScope, tenantId, subscription.Id, subscription.EffectivePrice, subscription.Currency, daysOverdue: 5);

        using (IServiceScope firstApplyScope = _factory.Services.CreateScope())
        {
            ApplyOrganizationSubscriptionBillingDecisionResult first = await firstApplyScope.ServiceProvider
                .GetRequiredService<ISender>()
                .Send(new ApplyOrganizationSubscriptionBillingDecisionCommand(tenantId), CancellationToken.None);
            first.ResultingStatus.Should().Be(OrganizationSubscriptionStatus.PastDue);
        }

        using IServiceScope secondApplyScope = _factory.Services.CreateScope();
        ApplyOrganizationSubscriptionBillingDecisionResult second = await secondApplyScope.ServiceProvider
            .GetRequiredService<ISender>()
            .Send(new ApplyOrganizationSubscriptionBillingDecisionCommand(tenantId), CancellationToken.None);

        second.TransitionApplied.Should().BeFalse();
        second.Recommendation.Should().Be(BillingLifecycleRecommendedAction.NoAction);
        second.PreviousStatus.Should().Be(OrganizationSubscriptionStatus.PastDue);
        second.ResultingStatus.Should().Be(OrganizationSubscriptionStatus.PastDue);

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription reloaded = (await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetByIdAsync(subscription.Id, CancellationToken.None))!;
        reloaded.Status.Should().Be(OrganizationSubscriptionStatus.PastDue);
    }
}
