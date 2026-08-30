using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Api.Endpoints;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Seed;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Proves the ADR-033 Task 07 session/bootstrap entitlement contract end-to-end through real HTTP
/// login/register/refresh calls backed by the real <c>ITenantEntitlementService</c> resolver (no mocking
/// of the entitlement algorithm) — grandfathered, normal subscribed, no-subscription, Reserved, and
/// cross-tenant isolation cases (Task 07 §36), plus freshness across a token refresh (Task 07 §16/§37).
///
/// Needs a Docker daemon, same disclosed limitation as every other PostgresApiFactory-backed test.
/// </summary>
public class TenantEntitlementSessionContractDbTests : IClassFixture<PostgresApiFactory>
{
    private const string NonCoreFeatureKey = "security.gates";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresApiFactory _factory;

    public TenantEntitlementSessionContractDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureFeatureCatalogueSeededAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IFeatureCatalogueSeeder>().SeedAsync(CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

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

    private async Task<SubscriptionPackageVersionId> SeedPackageVersionGrantingAsync(string code, string featureKey)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        ISubscriptionPackageFeatureRepository packageFeatures = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageFeatureRepository>();
        IFeatureDefinitionRepository featureDefinitions = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<FeatureDefinition> catalogue = await featureDefinitions.GetAllAsync(CancellationToken.None);
        FeatureDefinition feature = catalogue.Single(f => f.Key == featureKey);

        SubscriptionPackage package = SubscriptionPackage.Create(code, code, null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, StartDate, null, 1000m, null, null, 10000m, "BDT");

        packages.Add(package);
        versions.Add(version);
        packageFeatures.Add(new SubscriptionPackageFeature(version.Id, feature.Id, enabled: true, limitValue: null));

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        return version.Id;
    }

    private async Task SeedSubscriptionAsync(Guid tenantId, SubscriptionPackageVersionId versionId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IOrganizationSubscriptionRepository subscriptions = scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenantId, versionId, BillingCycle.Monthly, StartDate, null, null, 1000m, 0m, "BDT", ActivatedAt, autoRenew: false);
        subscriptions.Add(subscription);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<HttpResponseMessage> RegisterRawAsync(HttpClient client, Guid tenantId, string email) =>
        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            tenantId,
            email,
            password = "Correct-Horse-Battery-9",
            fullName = "Test User",
            phoneNumber = (string?)null,
        });

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, Guid tenantId, string email)
    {
        HttpResponseMessage response = await RegisterRawAsync(client, tenantId, email);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthResponse? auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        auth.Should().NotBeNull();
        return auth!;
    }

    [Fact]
    public async Task NoSubscription_Tenant_Has_Core_Enabled_And_NonCore_Disabled()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "entitlement-nosub";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using HttpClient client = _factory.CreateClient();
        AuthResponse auth = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        auth.User.Entitlements.Should().NotBeEmpty();
        auth.User.Entitlements.Should().Contain(e => e.FeatureKey == "property" && e.Enabled);
        auth.User.Entitlements.Should().Contain(e => e.FeatureKey == NonCoreFeatureKey && !e.Enabled);
    }

    [Fact]
    public async Task Subscribed_Tenant_Reflects_Package_Composition_In_Entitlements()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "entitlement-subscribed";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionGrantingAsync($"pkg-{slug}", NonCoreFeatureKey);
        await SeedSubscriptionAsync(tenantId, versionId);

        using HttpClient client = _factory.CreateClient();
        AuthResponse auth = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        auth.User.Entitlements.Should().Contain(e => e.FeatureKey == NonCoreFeatureKey && e.Enabled);
    }

    [Fact]
    public async Task Reserved_Feature_Is_Never_Returned_As_Enabled()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "entitlement-reserved";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using IServiceScope scope = _factory.Services.CreateScope();
        IFeatureDefinitionRepository featureDefinitions = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        List<FeatureDefinition> catalogue = await featureDefinitions.GetAllAsync(CancellationToken.None);
        List<FeatureDefinition> reserved = catalogue.Where(f => f.Status == FeatureCatalogueStatus.Reserved).ToList();
        reserved.Should().NotBeEmpty("the seeded catalogue must contain at least one Reserved feature for this test to be meaningful");

        using HttpClient client = _factory.CreateClient();
        AuthResponse auth = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        foreach (FeatureDefinition feature in reserved)
        {
            auth.User.Entitlements.Should().Contain(e => e.FeatureKey == feature.Key && !e.Enabled);
        }
    }

    [Fact]
    public async Task Grandfathered_Tenant_Receives_All_Active_Features_Enabled()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "entitlement-grandfathered";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            LegacyMigrationSubscriptionBackfillSeeder seeder = new(
                scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
            await seeder.SeedAsync(CancellationToken.None);
        }

        using IServiceScope catalogueScope = _factory.Services.CreateScope();
        List<FeatureDefinition> activeFeatures = (await catalogueScope.ServiceProvider
                .GetRequiredService<IFeatureDefinitionRepository>().GetAllAsync(CancellationToken.None))
            .Where(f => f.Status == FeatureCatalogueStatus.Active)
            .ToList();

        using HttpClient client = _factory.CreateClient();
        AuthResponse auth = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        foreach (FeatureDefinition feature in activeFeatures)
        {
            auth.User.Entitlements.Should().Contain(e => e.FeatureKey == feature.Key && e.Enabled);
        }
    }

    [Fact]
    public async Task Tenant_A_Entitlements_Do_Not_Leak_Into_Tenant_Bs_Session()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slugA = "entitlement-isolation-a";
        string slugB = "entitlement-isolation-b";
        Guid tenantAId = await SeedActiveTenantAsync(slugA);
        Guid tenantBId = await SeedActiveTenantAsync(slugB);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionGrantingAsync($"pkg-{slugA}", NonCoreFeatureKey);
        await SeedSubscriptionAsync(tenantAId, versionId);
        // Tenant B is deliberately left without a subscription.

        using HttpClient clientA = _factory.CreateClient();
        using HttpClient clientB = _factory.CreateClient();
        AuthResponse authA = await RegisterAsync(clientA, tenantAId, $"admin-{slugA}@example.com");
        AuthResponse authB = await RegisterAsync(clientB, tenantBId, $"admin-{slugB}@example.com");

        authA.User.Entitlements.Should().Contain(e => e.FeatureKey == NonCoreFeatureKey && e.Enabled);
        authB.User.Entitlements.Should().Contain(e => e.FeatureKey == NonCoreFeatureKey && !e.Enabled);
    }

    [Fact]
    public async Task Refresh_Returns_Up_To_Date_Entitlements_After_A_Subscription_Change()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "entitlement-refresh-freshness";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using HttpClient client = _factory.CreateClient();
        AuthResponse initial = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");
        initial.User.Entitlements.Should().Contain(e => e.FeatureKey == NonCoreFeatureKey && !e.Enabled);

        SubscriptionPackageVersionId versionId = await SeedPackageVersionGrantingAsync($"pkg-{slug}", NonCoreFeatureKey);
        await SeedSubscriptionAsync(tenantId, versionId);

        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { tenantId });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponse? refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);

        refreshed.Should().NotBeNull();
        refreshed!.User.Entitlements.Should().Contain(e => e.FeatureKey == NonCoreFeatureKey && e.Enabled);
    }
}
