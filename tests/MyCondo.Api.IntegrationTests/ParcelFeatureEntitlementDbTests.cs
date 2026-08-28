using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Seed;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof that ADR-033 Task 09's rollout composes correctly for a representative non-core,
/// non-Gates feature (<c>security.parcels</c>) — <see cref="GateFeatureEntitlementDbTests"/> already
/// proves the resolver/behavior plumbing itself in depth for the Task 06 pilot slice; this class instead
/// proves the same composition holds for a feature marked in this rollout, plus one core-feature safety
/// proof (Task 09 §49) showing the rollout did not accidentally make a core capability
/// subscription-dependent. One representative endpoint (<c>GET /api/v1/parcels</c>, permission
/// <c>parcel.view</c>) stands in for the whole newly-marked slice.
///
/// Needs a Docker daemon, same disclosed limitation as every other PostgresApiFactory-backed test.
/// </summary>
public class ParcelFeatureEntitlementDbTests : IClassFixture<PostgresApiFactory>
{
    private const string ParcelsFeatureKey = "security.parcels";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresApiFactory _factory;

    public ParcelFeatureEntitlementDbTests(PostgresApiFactory factory)
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

    private async Task<SubscriptionPackageVersionId> SeedPackageVersionAsync(string code, bool grantsParcels)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        ISubscriptionPackageFeatureRepository packageFeatures = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageFeatureRepository>();
        IFeatureDefinitionRepository featureDefinitions = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<FeatureDefinition> catalogue = await featureDefinitions.GetAllAsync(CancellationToken.None);
        FeatureDefinition parcelsFeature = catalogue.Single(f => f.Key == ParcelsFeatureKey);

        SubscriptionPackage package = SubscriptionPackage.Create(code, code, null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, StartDate, null, 1000m, null, null, 10000m, "BDT");

        packages.Add(package);
        versions.Add(version);
        packageFeatures.Add(new SubscriptionPackageFeature(version.Id, parcelsFeature.Id, grantsParcels, limitValue: null));

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

    private async Task SeedOverrideAsync(Guid tenantId, bool enabled)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IFeatureDefinitionRepository featureDefinitions = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        ITenantFeatureOverrideRepository overrides = scope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<FeatureDefinition> catalogue = await featureDefinitions.GetAllAsync(CancellationToken.None);
        FeatureDefinition parcelsFeature = catalogue.Single(f => f.Key == ParcelsFeatureKey);

        overrides.Add(TenantFeatureOverride.Create(
            tenantId, parcelsFeature, enabled, effectiveFrom: ActivatedAt, effectiveUntil: null,
            reason: "test", createdBy: Guid.NewGuid(), createdAtUtc: ActivatedAt));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
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

    private static async Task<HttpResponseMessage> GetParcelsAsync(HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/parcels?page=1&pageSize=20");
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> GetResidentsAsync(HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/residents?page=1&pageSize=20");
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Entitled_Tenant_With_Permission_Succeeds()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "parcels-entitled";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync($"pkg-{slug}", grantsParcels: true);
        await SeedSubscriptionAsync(tenantId, versionId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetParcelsAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NoSubscription_With_Permission_Returns_403_FeatureNotEntitled()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "parcels-nosub";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetParcelsAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("feature_not_entitled");
    }

    [Fact]
    public async Task Package_Not_Granting_The_Feature_Returns_403_FeatureNotEntitled()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "parcels-notgranted";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync($"pkg-{slug}", grantsParcels: false);
        await SeedSubscriptionAsync(tenantId, versionId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetParcelsAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("feature_not_entitled");
    }

    [Fact]
    public async Task Entitled_Tenant_Without_Permission_Returns_403_Without_The_FeatureNotEntitled_Code()
    {
        // Proves entitlement and RBAC remain separate gates for a feature marked in Task 09 (not just the
        // Task 06 pilot): a package that grants the feature must not make an otherwise-unauthorized user
        // able to reach it, and the failure reason must be the existing RBAC denial.
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "parcels-nopermission";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync($"pkg-{slug}", grantsParcels: true);
        await SeedSubscriptionAsync(tenantId, versionId);

        using HttpClient bootstrapClient = _factory.CreateClient();
        await RegisterAsync(bootstrapClient, tenantId, $"admin-{slug}@example.com"); // first user = OrganizationAdmin

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto secondUserTokens = await RegisterAsync(client, tenantId, $"no-permissions-{slug}@example.com");

        HttpResponseMessage response = await GetParcelsAsync(client, secondUserTokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("feature_not_entitled");
    }

    [Fact]
    public async Task Override_Disabling_The_Feature_Wins_Over_A_Package_That_Grants_It()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "parcels-override-disable";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync($"pkg-{slug}", grantsParcels: true);
        await SeedSubscriptionAsync(tenantId, versionId);
        await SeedOverrideAsync(tenantId, enabled: false);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetParcelsAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("feature_not_entitled");
    }

    [Fact]
    public async Task Grandfathered_Tenant_Keeps_Access_After_Enforcement_Lands()
    {
        // Task 04's no-loss invariant must survive a feature newly marked in this rollout, not just the
        // Task 06 pilot: a tenant migrated onto the LEGACY-MIGRATION-GRANDFATHERED package (which grants
        // every Active feature) must still reach a Task 09-gated endpoint.
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "parcels-grandfathered";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            LegacyMigrationSubscriptionBackfillSeeder seeder = new(
                scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
            await seeder.SeedAsync(CancellationToken.None);
        }

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetParcelsAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Core_Feature_Succeeds_With_Permission_And_No_Subscription_At_All()
    {
        // Task 09 §49: at least one marked-core feature must demonstrate that Task 09's rollout did not
        // accidentally make a core capability subscription-dependent. residents.directory is IsCore=true
        // in the Feature Catalogue and (per this rollout's own analysis) intentionally carries no
        // IRequiresFeature marker on any Residents request — this proves that holds at the HTTP level:
        // a tenant with zero OrganizationSubscription rows can still reach it.
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "residents-core-nosub";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetResidentsAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
