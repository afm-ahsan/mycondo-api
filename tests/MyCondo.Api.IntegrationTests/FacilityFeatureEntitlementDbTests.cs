using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Tenancy;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;
using MyCondo.Infrastructure.Seed;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip proof of ADR-033 Task 09A's resource-derived resolution for the Community Hall / Swimming
/// Pool gap identified in Task 09: <c>GetFacilityByIdQuery</c> (permission <c>facility.view</c>) is the
/// same request type for both facility types, so its FeatureKey cannot be a static
/// <c>IRequiresFeature</c> — it is resolved per-record via <c>FacilityFeatureResolver</c>
/// (<see cref="MyCondo.Application.Features.Amenities.Common.FacilityFeatureResolver{TRequest}"/>). The
/// mandatory proof (Task 09A §29/§37) is
/// <see cref="Same_Request_Type_Grants_Or_Denies_Per_Record_By_Facility_Type"/>: one package granting only
/// Community Hall must allow a Community Hall facility and deny a Swimming Pool facility read through the
/// exact same endpoint.
///
/// Needs a Docker daemon, same disclosed limitation as every other PostgresApiFactory-backed test.
/// </summary>
public class FacilityFeatureEntitlementDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresApiFactory _factory;

    public FacilityFeatureEntitlementDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureFeatureCatalogueSeededAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IFeatureCatalogueSeeder>().SeedAsync(CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
    }

    // Building/Facility rows are RLS-protected — a DbContext resolved from the ambient scope has no
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

    private async Task<Guid> SeedFacilityAsync(Guid tenantId, Guid buildingId, FacilityType facilityType)
    {
        DateTimeOffset now;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            now = scope.ServiceProvider.GetRequiredService<IClock>().UtcNow;
        }

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        FacilityRepository facilities = new(db);
        Facility facility = Facility.Create(
            tenantId, new BuildingId(buildingId), $"{facilityType}-{Guid.NewGuid():N}", facilityType, capacity: 10,
            operatingHoursStart: null, operatingHoursEnd: null, requiresApproval: false, bookingChargeAmount: null,
            depositAmount: null, cancellationDeadlineHours: 24, cancellationDeductionPercentage: 0m,
            guestFeeAmount: null, minimumAgeUnaccompanied: null, requiresSafetyAcknowledgement: false,
            blocksEntryIfAccountOverdue: false, nowUtc: now);
        facilities.Add(facility);
        await db.SaveChangesAsync(CancellationToken.None);

        return facility.Id.Value;
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

    private static async Task<HttpResponseMessage> GetFacilityAsync(HttpClient client, string accessToken, Guid facilityId)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/facilities/{facilityId}");
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Same_Request_Type_Grants_Or_Denies_Per_Record_By_Facility_Type()
    {
        // Task 09A §29/§37 — the central proof: one shared request type (GetFacilityByIdQuery), one
        // tenant, one package granting facilities.community_hall but NOT facilities.swimming_pool. The
        // outcome must differ per-record.
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "facility-differentiation";
        (Guid tenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync(slug);
        Guid communityHallId = await SeedFacilityAsync(tenantId, buildingId, FacilityType.CommunityHall);
        Guid poolId = await SeedFacilityAsync(tenantId, buildingId, FacilityType.SwimmingPool);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync(
            $"pkg-{slug}", ("facilities.community_hall", true), ("facilities.swimming_pool", false));
        await SeedSubscriptionAsync(tenantId, versionId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage communityHallResponse = await GetFacilityAsync(client, tokens.AccessToken, communityHallId);
        HttpResponseMessage poolResponse = await GetFacilityAsync(client, tokens.AccessToken, poolId);

        communityHallResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        poolResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string poolBody = await poolResponse.Content.ReadAsStringAsync();
        poolBody.Should().Contain("feature_not_entitled");
    }

    [Fact]
    public async Task NoSubscription_With_Permission_Returns_403_FeatureNotEntitled()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "facility-nosub";
        (Guid tenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync(slug);
        Guid facilityId = await SeedFacilityAsync(tenantId, buildingId, FacilityType.CommunityHall);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetFacilityAsync(client, tokens.AccessToken, facilityId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("feature_not_entitled");
    }

    [Fact]
    public async Task Cross_Tenant_Facility_Id_Returns_404_Not_403()
    {
        // Task 09A §32 — a cross-tenant id must not reveal entitlement/resource classification; it must
        // preserve the same not-found outcome the handler already produces, never feature_not_entitled.
        await EnsureFeatureCatalogueSeededAsync();
        (Guid ownerTenantId, _) = await SeedActiveTenantWithBuildingAsync("facility-cross-owner");
        (Guid otherTenantId, Guid otherBuildingId) = await SeedActiveTenantWithBuildingAsync("facility-cross-other");
        Guid otherFacilityId = await SeedFacilityAsync(otherTenantId, otherBuildingId, FacilityType.CommunityHall);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync(
            "pkg-facility-cross-owner", ("facilities.community_hall", true), ("facilities.swimming_pool", true));
        await SeedSubscriptionAsync(ownerTenantId, versionId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, ownerTenantId, "admin-facility-cross-owner@example.com");

        HttpResponseMessage response = await GetFacilityAsync(client, tokens.AccessToken, otherFacilityId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Entitled_Tenant_Without_Permission_Returns_403_Without_The_FeatureNotEntitled_Code()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "facility-nopermission";
        (Guid tenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync(slug);
        Guid facilityId = await SeedFacilityAsync(tenantId, buildingId, FacilityType.CommunityHall);
        SubscriptionPackageVersionId versionId = await SeedPackageVersionAsync(
            $"pkg-{slug}", ("facilities.community_hall", true));
        await SeedSubscriptionAsync(tenantId, versionId);

        using HttpClient bootstrapClient = _factory.CreateClient();
        await RegisterAsync(bootstrapClient, tenantId, $"admin-{slug}@example.com"); // first user = OrganizationAdmin

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto secondUserTokens = await RegisterAsync(client, tenantId, $"no-permissions-{slug}@example.com");

        HttpResponseMessage response = await GetFacilityAsync(client, secondUserTokens.AccessToken, facilityId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("feature_not_entitled");
    }

    [Fact]
    public async Task Grandfathered_Tenant_Reads_Both_Facility_Types()
    {
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "facility-grandfathered";
        (Guid tenantId, Guid buildingId) = await SeedActiveTenantWithBuildingAsync(slug);
        Guid communityHallId = await SeedFacilityAsync(tenantId, buildingId, FacilityType.CommunityHall);
        Guid poolId = await SeedFacilityAsync(tenantId, buildingId, FacilityType.SwimmingPool);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            LegacyMigrationSubscriptionBackfillSeeder seeder = new(
                scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
            await seeder.SeedAsync(CancellationToken.None);
        }

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage communityHallResponse = await GetFacilityAsync(client, tokens.AccessToken, communityHallId);
        HttpResponseMessage poolResponse = await GetFacilityAsync(client, tokens.AccessToken, poolId);

        communityHallResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        poolResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
