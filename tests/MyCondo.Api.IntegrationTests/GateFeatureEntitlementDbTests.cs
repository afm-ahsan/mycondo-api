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
/// Round-trip proof that <c>FeatureEntitlementBehavior</c> (ADR-033 §16, Task 06) is actually reached via
/// a real HTTP request and composes correctly with the existing RBAC/tenant-context gates, using
/// <c>security.gates</c> (Task 06's pilot feature — non-core, Active, RBAC-mature) as the exercised
/// request. One representative endpoint (<c>GET /api/v1/properties/gates</c>, permission <c>gate.view</c>)
/// stands in for the whole pilot slice; <c>FeatureEntitlementBehaviorTests</c> already covers the
/// behavior's own logic in isolation with fakes — this class instead proves the real wiring: DI
/// registration order, exception→403 mapping, and RBAC-vs-entitlement composition through actual
/// endpoint filters.
///
/// Needs a Docker daemon, same disclosed limitation as every other PostgresApiFactory-backed test.
/// </summary>
public class GateFeatureEntitlementDbTests : IClassFixture<PostgresApiFactory>
{
    private const string FeatureKey = "security.gates";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresApiFactory _factory;

    public GateFeatureEntitlementDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureFeatureCatalogueSeededAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IFeatureCatalogueSeeder>().SeedAsync(CancellationToken.None);
        // FeatureCatalogueSeeder has no IUnitOfWork of its own (DatabaseSeederExtensions saves after
        // every seeder runs at real startup) — this test invokes it standalone, so it must save explicitly.
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

    private async Task<SubscriptionPackageVersionId> SeedPackageVersionAsync(string code, bool grantsGates)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        ISubscriptionPackageFeatureRepository packageFeatures = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageFeatureRepository>();
        IFeatureDefinitionRepository featureDefinitions = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<FeatureDefinition> catalogue = await featureDefinitions.GetAllAsync(CancellationToken.None);
        FeatureDefinition gatesFeature = catalogue.Single(f => f.Key == FeatureKey);

        SubscriptionPackage package = SubscriptionPackage.Create(code, code, null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, StartDate, null, 1000m, null, null, 10000m, "BDT");

        packages.Add(package);
        versions.Add(version);
        packageFeatures.Add(new SubscriptionPackageFeature(version.Id, gatesFeature.Id, grantsGates, limitValue: null));

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
        FeatureDefinition gatesFeature = catalogue.Single(f => f.Key == FeatureKey);

        overrides.Add(TenantFeatureOverride.Create(
            tenantId, gatesFeature, enabled, effectiveFrom: ActivatedAt, effectiveUntil: null,
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

    private static async Task<HttpResponseMessage> GetGatesAsync(HttpClient client, string accessToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/properties/gates?activeOnly=false");
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Entitled_Tenant_With_Permission_Succeeds()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "gates-entitled";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync($"pkg-{slug}", grantsGates: true);
        await SeedSubscriptionAsync(tenantId, versionId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetGatesAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NoSubscription_With_Permission_Returns_403_FeatureNotEntitled()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "gates-nosub";
        Guid tenantId = await SeedActiveTenantAsync(slug);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetGatesAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("feature_not_entitled");
    }

    [Fact]
    public async Task Package_Not_Granting_The_Feature_Returns_403_FeatureNotEntitled()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "gates-notgranted";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync($"pkg-{slug}", grantsGates: false);
        await SeedSubscriptionAsync(tenantId, versionId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetGatesAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("feature_not_entitled");
    }

    [Fact]
    public async Task Entitled_Tenant_Without_Permission_Returns_403_Without_The_FeatureNotEntitled_Code()
    {
        // Proves entitlement and RBAC remain separate gates (Task 06 §33): a package that grants the
        // feature must not make an otherwise-unauthorized user able to reach it, and the failure reason
        // must be the existing RBAC denial, not feature_not_entitled.
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "gates-nopermission";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync($"pkg-{slug}", grantsGates: true);
        await SeedSubscriptionAsync(tenantId, versionId);

        using HttpClient bootstrapClient = _factory.CreateClient();
        await RegisterAsync(bootstrapClient, tenantId, $"admin-{slug}@example.com"); // first user = OrganizationAdmin

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto secondUserTokens = await RegisterAsync(client, tenantId, $"no-permissions-{slug}@example.com");

        HttpResponseMessage response = await GetGatesAsync(client, secondUserTokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("feature_not_entitled");
    }

    [Fact]
    public async Task Override_Enabling_The_Feature_Wins_Over_A_Package_That_Does_Not_Grant_It()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "gates-override-enable";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync($"pkg-{slug}", grantsGates: false);
        await SeedSubscriptionAsync(tenantId, versionId);
        await SeedOverrideAsync(tenantId, enabled: true);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetGatesAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Override_Disabling_The_Feature_Wins_Over_A_Package_That_Grants_It()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "gates-override-disable";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync($"pkg-{slug}", grantsGates: true);
        await SeedSubscriptionAsync(tenantId, versionId);
        await SeedOverrideAsync(tenantId, enabled: false);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetGatesAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("feature_not_entitled");
    }

    [Fact]
    public async Task Grandfathered_Tenant_Keeps_Access_After_Enforcement_Lands()
    {
        // Task 06 §38: the Task 04 no-loss invariant must survive actual enforcement — a tenant migrated
        // onto the LEGACY-MIGRATION-GRANDFATHERED package (which grants every Active feature) must still
        // reach a pilot-gated endpoint once FeatureEntitlementBehavior is live.
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "gates-grandfathered";
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

        HttpResponseMessage response = await GetGatesAsync(client, tokens.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
