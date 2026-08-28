using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory) for the
/// OrganizationSubscription foundation (ADR-033 Task 03) — no RLS on the <c>platform.organization_subscriptions</c>
/// table (platform-schema, not tenant data), same reasoning as SubscriptionPackageDbTests. These need a
/// Docker daemon and were NOT executed in the environment they were authored in — see PostgresApiFactory's
/// doc comment. Run wherever Docker is available before trusting them.
/// </summary>
public class OrganizationSubscriptionDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public OrganizationSubscriptionDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static async Task<SubscriptionPackageVersion> CreatePackageVersionAsync(IServiceScope scope)
    {
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{Guid.NewGuid():N}", "Professional", null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, StartDate, null, 8000.50m, null, null, 80000m, "BDT");

        packages.Add(package);
        versions.Add(version);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return version;
    }

    [Fact]
    public async Task OrganizationSubscription_Persists_And_Round_Trips()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreatePackageVersionAsync(scope);

        IOrganizationSubscriptionRepository subscriptions = scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenantId, version.Id, BillingCycle.Monthly, StartDate, null, StartDate.AddMonths(1),
            8000.50m, 500.25m, "BDT", ActivatedAt, autoRenew: true);

        subscriptions.Add(subscription);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription? reloaded = await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetByIdAsync(subscription.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.TenantId.Should().Be(tenantId);
        reloaded.PackageVersionId.Should().Be(version.Id);
        reloaded.Status.Should().Be(OrganizationSubscriptionStatus.Active);
        reloaded.BillingCycle.Should().Be(BillingCycle.Monthly);
        reloaded.StartDate.Should().Be(StartDate);
        reloaded.NextBillingDate.Should().Be(StartDate.AddMonths(1));
        reloaded.BasePrice.Should().Be(8000.50m);
        reloaded.Discount.Should().Be(500.25m);
        reloaded.EffectivePrice.Should().Be(7500.25m);
        reloaded.Currency.Should().Be("BDT");
        reloaded.ActivatedAt.Should().Be(ActivatedAt);
        reloaded.AutoRenew.Should().BeTrue();
    }

    [Fact]
    public async Task OrganizationSubscription_Lifecycle_Transitions_Persist()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreatePackageVersionAsync(scope);

        IOrganizationSubscriptionRepository subscriptions = scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        OrganizationSubscription subscription = OrganizationSubscription.Create(
            Guid.NewGuid(), version.Id, BillingCycle.Monthly, StartDate, null, null, 8000m, 0m, "BDT", ActivatedAt, false);
        subscriptions.Add(subscription);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        subscription.MarkPastDue();
        DateTimeOffset restrictedAt = ActivatedAt.AddDays(30);
        subscription.Restrict(restrictedAt);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription? reloaded = await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetByIdAsync(subscription.Id, CancellationToken.None);

        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(OrganizationSubscriptionStatus.Restricted);
        reloaded.RestrictedAt.Should().Be(restrictedAt);
    }

    [Fact]
    public async Task GetCurrentForTenantAsync_Returns_Only_Non_Terminal_Subscription()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreatePackageVersionAsync(scope);

        IOrganizationSubscriptionRepository subscriptions = scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenantId, version.Id, BillingCycle.Monthly, StartDate, null, null, 8000m, 0m, "BDT", ActivatedAt, false);
        subscriptions.Add(subscription);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        OrganizationSubscription? current = await readScope.ServiceProvider
            .GetRequiredService<IOrganizationSubscriptionRepository>()
            .GetCurrentForTenantAsync(tenantId, CancellationToken.None);

        current.Should().NotBeNull();
        current!.Id.Should().Be(subscription.Id);
    }

    [Fact]
    public async Task OrganizationSubscription_Rejects_A_Second_Current_Subscription_For_The_Same_Tenant()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreatePackageVersionAsync(scope);

        IOrganizationSubscriptionRepository subscriptions = scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription first = OrganizationSubscription.Create(
            tenantId, version.Id, BillingCycle.Monthly, StartDate, null, null, 8000m, 0m, "BDT", ActivatedAt, false);
        subscriptions.Add(first);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope secondScope = _factory.Services.CreateScope();
        IOrganizationSubscriptionRepository secondSubscriptions = secondScope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork secondUnitOfWork = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        OrganizationSubscription second = OrganizationSubscription.Create(
            tenantId, version.Id, BillingCycle.Monthly, StartDate.AddDays(1), null, null, 8000m, 0m, "BDT", ActivatedAt, false);
        secondSubscriptions.Add(second);

        Func<Task> act = () => secondUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task OrganizationSubscription_Allows_A_New_Current_Subscription_After_The_Prior_One_Expires()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        SubscriptionPackageVersion version = await CreatePackageVersionAsync(scope);

        IOrganizationSubscriptionRepository subscriptions = scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription first = OrganizationSubscription.Create(
            tenantId, version.Id, BillingCycle.Monthly, StartDate, null, null, 8000m, 0m, "BDT", ActivatedAt, false);
        first.Cancel(ActivatedAt.AddDays(10));
        first.Expire(ActivatedAt.AddDays(40));
        subscriptions.Add(first);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope secondScope = _factory.Services.CreateScope();
        IOrganizationSubscriptionRepository secondSubscriptions = secondScope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork secondUnitOfWork = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        OrganizationSubscription second = OrganizationSubscription.Create(
            tenantId, version.Id, BillingCycle.Monthly, StartDate.AddMonths(2),
            null, null, 8000m, 0m, "BDT", ActivatedAt.AddDays(41), false);
        secondSubscriptions.Add(second);

        Func<Task> act = () => secondUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
