using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
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
/// Mandatory representative Restricted-mode proof matrix for the ADR-032 Task 10A read/write classification
/// rollout (Task 10A §39-§46) — the pilot set already covered by <see cref="TenantLifecycleEnforcementDbTests"/>
/// proves the *mechanism*; this class proves the *rollout* actually reaches the newly-classified requests:
/// a report/export endpoint, a detail-by-id endpoint, a non-core feature-gated read composing with
/// entitlement, an ordinary RBAC denial under Restricted, and a resource-derived (Task 09A) read.
///
/// Needs a Docker daemon, same disclosed limitation as every other PostgresApiFactory-backed test.
/// </summary>
public class LifecycleClassificationRolloutDbTests : IClassFixture<PostgresApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresApiFactory _factory;

    public LifecycleClassificationRolloutDbTests(PostgresApiFactory factory)
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

    // Building/Facility rows are RLS-protected — see FacilityFeatureEntitlementDbTests for why the tenant
    // is seeded via the ambient scope but the building via a tenant-scoped DbContext.
    private async Task<Guid> SeedBuildingAsync(Guid tenantId, string slug)
    {
        DateTimeOffset now;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            now = scope.ServiceProvider.GetRequiredService<IClock>().UtcNow;
        }

        await using MyCondoDbContext db = _factory.CreateDbContextForTenant(tenantId);
        BuildingRepository buildings = new(db);
        Building building = Building.Create(tenantId, $"Building {slug}", Guid.NewGuid().ToString("N")[..8], null, now);
        buildings.Add(building);
        await db.SaveChangesAsync(CancellationToken.None);

        return building.Id.Value;
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

    // Sets the subscription's OrganizationSubscriptionStatus without granting any package features — the
    // dangling SubscriptionPackageVersionId matches TenantLifecycleEnforcementDbTests' convention: entitlement
    // resolution against a non-existent version correctly resolves to "not entitled" for any non-core feature.
    private async Task SeedRestrictedSubscriptionWithNoEntitlementsAsync(Guid tenantId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IOrganizationSubscriptionRepository subscriptions = scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenantId, SubscriptionPackageVersionId.New(), BillingCycle.Monthly, StartDate, null, null,
            1000m, 0m, "BDT", ActivatedAt, autoRenew: false);
        subscription.MarkPastDue();
        subscription.Restrict(ActivatedAt);

        subscriptions.Add(subscription);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    // A real package version granting only facilities.community_hall, with the resulting subscription
    // forced into Restricted status — proves Task 09A's resource-derived resolution still composes
    // correctly with Task 10's ReadOnly access mode.
    private async Task SeedRestrictedSubscriptionGrantingCommunityHallAsync(Guid tenantId, string slug)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        ISubscriptionPackageFeatureRepository packageFeatures = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageFeatureRepository>();
        IFeatureDefinitionRepository featureDefinitions = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IOrganizationSubscriptionRepository subscriptions = scope.ServiceProvider.GetRequiredService<IOrganizationSubscriptionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        List<FeatureDefinition> catalogue = await featureDefinitions.GetAllAsync(CancellationToken.None);
        FeatureDefinition communityHall = catalogue.Single(f => f.Key == "facilities.community_hall");

        SubscriptionPackage package = SubscriptionPackage.Create($"pkg-{slug}", $"pkg-{slug}", null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, StartDate, null, 1000m, null, null, 10000m, "BDT");
        packages.Add(package);
        versions.Add(version);
        packageFeatures.Add(new SubscriptionPackageFeature(version.Id, communityHall.Id, enabled: true, limitValue: null));

        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenantId, version.Id, BillingCycle.Monthly, StartDate, null, null, 1000m, 0m, "BDT", ActivatedAt, autoRenew: false);
        subscription.MarkPastDue();
        subscription.Restrict(ActivatedAt);
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

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string accessToken, string path)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        request.Headers.Authorization = new("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<string> CodeOf(HttpResponseMessage response) => await response.Content.ReadAsStringAsync();

    [Fact]
    public async Task Restricted_Subscription_Allows_A_Report_Export_Endpoint()
    {
        // Task 10A §41 — a Restricted tenant can successfully execute a report/export query, proving the
        // Finance/Reports read classification rollout beyond basic list/detail CRUD.
        string slug = "task10a-export";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedRestrictedSubscriptionWithNoEntitlementsAsync(tenantId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetAsync(
            client, tokens.AccessToken, "/api/v1/reports/finance/trial-balance/export?format=csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Restricted_Subscription_Allows_A_Detail_By_Id_Read_Endpoint()
    {
        // Task 10A §42 — a detail-by-id endpoint stays reachable under Restricted; a 404 for an unknown id
        // (rather than 403) proves the request reached the handler instead of being lifecycle-blocked.
        string slug = "task10a-detail";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedRestrictedSubscriptionWithNoEntitlementsAsync(tenantId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetAsync(client, tokens.AccessToken, $"/api/v1/residents/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Restricted_Subscription_Read_Denies_A_NonCore_Feature_Not_Entitled()
    {
        // Task 10A §43 — lifecycle permits the read (Restricted), but downstream feature entitlement is
        // still authoritative: an un-entitled non-core feature must deny with feature_not_entitled, never
        // subscription_restricted.
        string slug = "task10a-notentitled";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedRestrictedSubscriptionWithNoEntitlementsAsync(tenantId);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetAsync(client, tokens.AccessToken, "/api/v1/properties/gates?activeOnly=false");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await CodeOf(response);
        body.Should().Contain("feature_not_entitled");
        body.Should().NotContain("subscription_restricted");
    }

    [Fact]
    public async Task Restricted_Subscription_Read_Still_Enforces_Ordinary_RBAC_Denial()
    {
        // Task 10A §45 — Restricted + Read + a core (always-entitled) feature + a missing permission must
        // still be an ordinary RBAC denial, not a lifecycle- or entitlement-coded one.
        string slug = "task10a-rbac";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        await SeedRestrictedSubscriptionWithNoEntitlementsAsync(tenantId);

        using HttpClient bootstrapClient = _factory.CreateClient();
        await RegisterAsync(bootstrapClient, tenantId, $"admin-{slug}@example.com"); // first user = OrganizationAdmin

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto secondUserTokens = await RegisterAsync(client, tenantId, $"no-permissions-{slug}@example.com");

        HttpResponseMessage response = await GetAsync(client, secondUserTokens.AccessToken, "/api/v1/residents?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        string body = await CodeOf(response);
        body.Should().NotContain("subscription_restricted");
        body.Should().NotContain("feature_not_entitled");
        body.Should().NotContain("tenant_suspended");
    }

    [Fact]
    public async Task Restricted_Subscription_Allows_A_ResourceDerived_FeatureGated_Read()
    {
        // Task 10A §46 — re-runs Task 09A's resource-derived resolution (Community Hall vs Swimming Pool)
        // under a Restricted subscription: a package granting only community_hall must still allow a
        // Community Hall facility read through GetFacilityByIdQuery.
        await EnsureFeatureCatalogueSeededAsync();
        string slug = "task10a-resourcederived";
        Guid tenantId = await SeedActiveTenantAsync(slug);
        Guid buildingId = await SeedBuildingAsync(tenantId, slug);
        Guid communityHallId = await SeedFacilityAsync(tenantId, buildingId, FacilityType.CommunityHall);
        await SeedRestrictedSubscriptionGrantingCommunityHallAsync(tenantId, slug);

        using HttpClient client = _factory.CreateClient();
        AuthTokensDto tokens = await RegisterAsync(client, tenantId, $"admin-{slug}@example.com");

        HttpResponseMessage response = await GetAsync(client, tokens.AccessToken, $"/api/v1/facilities/{communityHallId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
