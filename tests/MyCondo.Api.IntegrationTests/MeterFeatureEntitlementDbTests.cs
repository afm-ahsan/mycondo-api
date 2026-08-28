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
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;
using MyCondo.Infrastructure.Seed;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof of ADR-033 Task 09A's resource-derived resolution for the Utilities per-record gap
/// identified in Task 09: <c>GetMeterAssignmentHistoryQuery</c> (<c>GET /meters/{id}/assignment-history</c>,
/// permission <c>utility.meter.view</c>) is the same request type for electricity and gas meters, so its
/// FeatureKey is resolved per-record via <c>MeterFeatureResolver</c>
/// (<see cref="MyCondo.Application.Features.Utilities.Common.MeterFeatureResolver{TRequest}"/>). The
/// mandatory proof (Task 09A §31) is
/// <see cref="Same_Request_Type_Grants_Or_Denies_Per_Record_By_Utility_Type"/>: one package granting only
/// Electricity must allow an electricity meter and deny a gas meter through the exact same endpoint.
///
/// Needs a Docker daemon, same disclosed limitation as every other PostgresApiFactory-backed test.
/// </summary>
public class MeterFeatureEntitlementDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresApiFactory _factory;

    public MeterFeatureEntitlementDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureFeatureCatalogueSeededAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IFeatureCatalogueSeeder>().SeedAsync(CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

    // Building/Meter rows are RLS-protected — a DbContext resolved from the ambient scope has no
    // JWT/HttpContext for the real TenantContextAccessor to read, so RLS would reject the insert. Tenant
    // rows aren't RLS-protected, so the tenant is created via the ambient scope, then
    // _factory.CreateDbContextForTenant gives an explicitly tenant-scoped DbContext for everything else
    // (same pattern as ConsumptionHistoryExportContentVerificationTests).
    private async Task<(Guid TenantId, Guid BuildingId)> SeedActiveTenantWithBuildingAsync(string slug)
    {
        Guid tenantId;
        DateTimeOffset now;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ITenantRepository tenants = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            IClock clock = scope.ServiceProvider.GetRequiredService<IClock>();

            Tenant tenant = Tenant.Provision($"Tenant {slug}", slug, clock.UtcNow);
            tenant.Activate(clock.UtcNow);
            tenants.Add(tenant);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            tenantId = tenant.Id.Value;
            now = clock.UtcNow;
        }

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        BuildingRepository buildings = new(db);
        Building building = Building.Create(tenantId, $"Building {slug}", Guid.NewGuid().ToString("N")[..8], null, now);
        buildings.Add(building);
        await db.SaveChangesAsync(CancellationToken.None);

        return (tenantId, building.Id.Value);
    }

    private async Task<Guid> SeedMeterAsync(Guid tenantId, Guid buildingId, UtilityType utilityType)
    {
        DateTimeOffset now;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            now = scope.ServiceProvider.GetRequiredService<IClock>().UtcNow;
        }

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        MeterRepository meters = new(db);
        Meter meter = Meter.Install(tenantId, new BuildingId(buildingId), utilityType, $"{utilityType}-{Guid.NewGuid():N}", now);
        meters.Add(meter);
        await db.SaveChangesAsync(CancellationToken.None);

        return meter.Id.Value;
    }

    private async Task<SubscriptionPackageVersionId> SeedPackageVersionAsync(
        string code, params (string FeatureKey, bool Grants)[] grants)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        ISubscriptionPackageFeatureRepository packageFeatures = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageFeatureRepository>();
        IFeatureDefinitionRepository featureDefinitions = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<FeatureDefinition> catalogue = await featureDefinitions.GetAllAsync(CancellationToken.None);

        SubscriptionPackage package = SubscriptionPackage.Create(code, code, null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, StartDate, null, 1000m, null, null, 10000m, "BDT");

        packages.Add(package);
        versions.Add(version);

        foreach ((string featureKey, bool grantsFeature) in grants)
        {
            FeatureDefinition feature = catalogue.Single(f => f.Key == featureKey);
            packageFeatures.Add(new SubscriptionPackageFeature(version.Id, feature.Id, grantsFeature, limitValue: null));
        }

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

    private static async Task<HttpResponseMessage> GetAssignmentHistoryAsync(HttpClient client, string accessToken, Guid meterId)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/meters/{meterId}/assignment-history");
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Same_Request_Type_Grants_Or_Denies_Per_Record_By_Utility_Type()
    {
        // Task 09A §31 — the central proof: one shared request type (GetMeterAssignmentHistoryQuery), one
        // tenant, one package granting utilities.electricity but NOT utilities.gas. The outcome must
        // differ per-record.
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "meter-differentiation";
        (Guid tenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync(slug);
        Guid electricityMeterId = await SeedMeterAsync(tenantId, buildingId, UtilityType.Electricity);
        Guid gasMeterId = await SeedMeterAsync(tenantId, buildingId, UtilityType.Gas);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync(
            $"pkg-{slug}", ("utilities.electricity", true), ("utilities.gas", false));
        await SeedSubscriptionAsync(tenantId, versionId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage electricityResponse = await GetAssignmentHistoryAsync(client, tokens.AccessToken, electricityMeterId);
        HttpResponseMessage gasResponse = await GetAssignmentHistoryAsync(client, tokens.AccessToken, gasMeterId);

        electricityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        gasResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string gasBody = await gasResponse.Content.ReadAsStringAsync();
        gasBody.Should().Contain("feature_not_entitled");
    }

    [Fact]
    public async Task NoSubscription_With_Permission_Returns_403_FeatureNotEntitled()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "meter-nosub";
        (Guid tenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync(slug);
        Guid meterId = await SeedMeterAsync(tenantId, buildingId, UtilityType.Electricity);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetAssignmentHistoryAsync(client, tokens.AccessToken, meterId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("feature_not_entitled");
    }

    [Fact]
    public async Task Cross_Tenant_Meter_Id_Returns_404_Not_403()
    {
        // Task 09A §32 — a cross-tenant id must not reveal entitlement/resource classification; it must
        // preserve the same not-found outcome the handler already produces, never feature_not_entitled.
        await EnsureFeatureCatalogueSeededAsync();
        (Guid ownerTenantId, _) = await SeedActiveTenantWithBuildingAsync("meter-cross-owner");
        (Guid otherTenantId, Guid otherBuildingId) = await SeedActiveTenantWithBuildingAsync("meter-cross-other");
        Guid otherMeterId = await SeedMeterAsync(otherTenantId, otherBuildingId, UtilityType.Electricity);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync(
            "pkg-meter-cross-owner", ("utilities.electricity", true), ("utilities.gas", true));
        await SeedSubscriptionAsync(ownerTenantId, versionId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, ownerTenantId, "admin-meter-cross-owner@example.com");

        HttpResponseMessage response = await GetAssignmentHistoryAsync(client, tokens.AccessToken, otherMeterId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Grandfathered_Tenant_Reads_Both_Utility_Types()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "meter-grandfathered";
        (Guid tenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync(slug);
        Guid electricityMeterId = await SeedMeterAsync(tenantId, buildingId, UtilityType.Electricity);
        Guid gasMeterId = await SeedMeterAsync(tenantId, buildingId, UtilityType.Gas);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            LegacyMigrationSubscriptionBackfillSeeder seeder = new(
                scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
            await seeder.SeedAsync(CancellationToken.None);
        }

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage electricityResponse = await GetAssignmentHistoryAsync(client, tokens.AccessToken, electricityMeterId);
        HttpResponseMessage gasResponse = await GetAssignmentHistoryAsync(client, tokens.AccessToken, gasMeterId);

        electricityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        gasResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
