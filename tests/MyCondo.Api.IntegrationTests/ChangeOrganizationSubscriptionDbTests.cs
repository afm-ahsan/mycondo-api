using AwesomeAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.ChangeOrganizationSubscription;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof for <see cref="ChangeOrganizationSubscriptionCommand"/> against a real, ephemeral
/// PostgreSQL container (ADR-033 Task 13B) — sends commands straight through <see cref="ISender"/> inside
/// a DI scope, the same bypass of the currently-broken 401 JWT/HTTP fixture that
/// <see cref="ProvisionOrganizationWithAdminDbTests"/> already establishes for Task 12B (see that class's
/// doc comment; not re-litigated here). Needs a Docker daemon — see <see cref="PostgresApiFactory"/>.
/// </summary>
public class ChangeOrganizationSubscriptionDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ChangeOrganizationSubscriptionDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    /// <summary>Monthly and Annual are priced; Quarterly/SemiAnnual are deliberately left unpriced so
    /// the same version can also prove the "unsupported cycle" rejection.</summary>
    private static async Task<SubscriptionPackageVersion> CreateAssignablePackageVersionAsync(
        IServiceScope scope, decimal monthlyPrice, decimal annualPrice, string currency = "BDT")
    {
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{Guid.NewGuid():N}", "Package", null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, EffectiveFrom, null,
            monthlyPrice: monthlyPrice, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: annualPrice,
            currency: currency);
        version.Activate();
        package.Activate();
        package.SetCurrentVersion(version.Id);

        packages.Add(package);
        versions.Add(version);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return version;
    }

    private static async Task<Guid> ProvisionOrganizationAsync(
        IServiceScope scope, Guid packageVersionId, string suffix)
    {
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        ProvisionOrganizationResult result = await sender.Send(
            new ProvisionOrganizationWithAdminCommand(
                Name: "Task 13B Integration Org",
                Code: $"T13B-{suffix.ToUpperInvariant()}",
                Slug: $"task13b-{suffix}",
                AdministratorFullName: "Test Admin",
                AdministratorEmail: $"admin@task13b-{suffix}.test",
                AdministratorPassword: "Correct-Horse-Battery-9",
                EnabledModuleKeys: ["billing", "payments"],
                SubscriptionPackageVersionId: packageVersionId,
                BillingCycle: BillingCycle.Monthly,
                AutoRenew: false),
            CancellationToken.None);

        return result.TenantId;
    }

    [Fact]
    public async Task Changes_The_Current_Subscription_To_The_Exact_Selected_Package_Version_Cycle_And_AutoRenew()
    {
        using IServiceScope setupScope = _factory.Services.CreateScope();
        SubscriptionPackageVersion originalVersion =
            await CreateAssignablePackageVersionAsync(setupScope, monthlyPrice: 8000m, annualPrice: 84000m);
        Guid tenantId = await ProvisionOrganizationAsync(setupScope, originalVersion.Id.Value, "change-success");

        using IServiceScope targetScope = _factory.Services.CreateScope();
        SubscriptionPackageVersion targetVersion =
            await CreateAssignablePackageVersionAsync(targetScope, monthlyPrice: 1500m, annualPrice: 15000m, currency: "USD");

        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(
            new ChangeOrganizationSubscriptionCommand(tenantId, targetVersion.Id.Value, BillingCycle.Annual, AutoRenew: true),
            CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription? subscription = await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetCurrentForTenantAsync(tenantId, CancellationToken.None);

        subscription.Should().NotBeNull();
        subscription!.PackageVersionId.Should().Be(targetVersion.Id);
        subscription.BillingCycle.Should().Be(BillingCycle.Annual);
        subscription.BasePrice.Should().Be(15000m);
        subscription.Discount.Should().Be(0m);
        subscription.EffectivePrice.Should().Be(15000m);
        subscription.Currency.Should().Be("USD");
        subscription.AutoRenew.Should().BeTrue();
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Active);
    }

    [Fact]
    public async Task Rejects_An_Unsupported_Billing_Cycle_And_Leaves_The_Subscription_Unchanged()
    {
        using IServiceScope setupScope = _factory.Services.CreateScope();
        SubscriptionPackageVersion originalVersion =
            await CreateAssignablePackageVersionAsync(setupScope, monthlyPrice: 8000m, annualPrice: 84000m);
        Guid tenantId = await ProvisionOrganizationAsync(setupScope, originalVersion.Id.Value, "change-rollback");

        using IServiceScope targetScope = _factory.Services.CreateScope();
        // Quarterly/SemiAnnual are unpriced on this version.
        SubscriptionPackageVersion targetVersion =
            await CreateAssignablePackageVersionAsync(targetScope, monthlyPrice: 1500m, annualPrice: 15000m);

        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Func<Task> act = async () => await sender.Send(
            new ChangeOrganizationSubscriptionCommand(tenantId, targetVersion.Id.Value, BillingCycle.Quarterly, AutoRenew: true),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedBillingCycleException>();

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription? subscription = await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetCurrentForTenantAsync(tenantId, CancellationToken.None);

        subscription!.PackageVersionId.Should().Be(originalVersion.Id);
        subscription.BillingCycle.Should().Be(BillingCycle.Monthly);
        subscription.BasePrice.Should().Be(8000m);
    }

    [Fact]
    public async Task Rejects_The_Legacy_Grandfathered_Package_And_Leaves_The_Subscription_Unchanged()
    {
        using IServiceScope setupScope = _factory.Services.CreateScope();
        SubscriptionPackageVersion originalVersion =
            await CreateAssignablePackageVersionAsync(setupScope, monthlyPrice: 8000m, annualPrice: 84000m);
        Guid tenantId = await ProvisionOrganizationAsync(setupScope, originalVersion.Id.Value, "change-legacy");

        using IServiceScope legacyScope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = legacyScope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = legacyScope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork legacyUnitOfWork = legacyScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage legacyPackage = SubscriptionPackage.Create(
            "LEGACY-MIGRATION-GRANDFATHERED", "Legacy Migration (Grandfathered)", null);
        SubscriptionPackageVersion legacyVersion = SubscriptionPackageVersion.Create(
            legacyPackage.Id, 1, EffectiveFrom, null,
            monthlyPrice: 0m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null, currency: "BDT");
        legacyVersion.Activate();
        legacyPackage.Activate();
        legacyPackage.SetCurrentVersion(legacyVersion.Id);
        packages.Add(legacyPackage);
        versions.Add(legacyVersion);
        await legacyUnitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope scope = _factory.Services.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Func<Task> act = async () => await sender.Send(
            new ChangeOrganizationSubscriptionCommand(tenantId, legacyVersion.Id.Value, BillingCycle.Monthly, AutoRenew: false),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription? subscription = await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetCurrentForTenantAsync(tenantId, CancellationToken.None);

        subscription!.PackageVersionId.Should().Be(originalVersion.Id);
    }
}
