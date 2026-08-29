using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.TenantFeatureOverrides;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory) for the
/// TenantFeatureOverride foundation (ADR-033 Task 03) — no RLS on the <c>platform.tenant_feature_overrides</c>
/// table (platform-schema, not tenant data), same reasoning as SubscriptionPackageDbTests. These need a
/// Docker daemon and were NOT executed in the environment they were authored in — see PostgresApiFactory's
/// doc comment. Run wherever Docker is available before trusting them.
/// </summary>
public class TenantFeatureOverrideDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public TenantFeatureOverrideDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly DateTimeOffset EffectiveFrom = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static async Task<FeatureDefinition> CreateFeatureAsync(IServiceScope scope)
    {
        IFeatureDefinitionRepository features = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        FeatureDefinition feature = FeatureDefinition.Create(
            $"test.{Guid.NewGuid():N}", "Test Feature", null, null, "test", FeatureCatalogueStatus.Active, false, 5000, FeatureEntitlementType.Boolean);

        features.Add(feature);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        return feature;
    }

    [Fact]
    public async Task TenantFeatureOverride_Persists_And_Round_Trips()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        FeatureDefinition feature = await CreateFeatureAsync(scope);

        ITenantFeatureOverrideRepository overrides = scope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        Guid createdBy = Guid.NewGuid();
        TenantFeatureOverride @override = TenantFeatureOverride.Create(
            tenantId, feature, true, EffectiveFrom, EffectiveFrom.AddMonths(3), "Pilot enterprise customer", createdBy, EffectiveFrom);

        overrides.Add(@override);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        List<TenantFeatureOverride> all = await readScope.ServiceProvider
            .GetRequiredService<ITenantFeatureOverrideRepository>()
            .GetForTenantAsync(tenantId, CancellationToken.None);

        TenantFeatureOverride reloaded = all.Should().ContainSingle(o => o.Id == @override.Id).Subject;
        reloaded.TenantId.Should().Be(tenantId);
        reloaded.FeatureId.Should().Be(feature.Id);
        reloaded.Enabled.Should().BeTrue();
        reloaded.EffectiveFrom.Should().Be(EffectiveFrom);
        reloaded.EffectiveUntil.Should().Be(EffectiveFrom.AddMonths(3));
        reloaded.Reason.Should().Be("Pilot enterprise customer");
        reloaded.CreatedBy.Should().Be(createdBy);
        reloaded.CreatedAt.Should().Be(EffectiveFrom);
    }

    [Fact]
    public async Task TenantFeatureOverride_Persists_Disable_Override_With_Open_Ended_Window()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        FeatureDefinition feature = await CreateFeatureAsync(scope);

        ITenantFeatureOverrideRepository overrides = scope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        TenantFeatureOverride @override = TenantFeatureOverride.Create(
            tenantId, feature, false, EffectiveFrom, null, "Temporary support exception", Guid.NewGuid(), EffectiveFrom);

        overrides.Add(@override);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        List<TenantFeatureOverride> all = await readScope.ServiceProvider
            .GetRequiredService<ITenantFeatureOverrideRepository>()
            .GetForTenantAsync(tenantId, CancellationToken.None);

        TenantFeatureOverride reloaded = all.Should().ContainSingle(o => o.Id == @override.Id).Subject;
        reloaded.Enabled.Should().BeFalse();
        reloaded.EffectiveUntil.Should().BeNull();
    }

    [Fact]
    public async Task TenantFeatureOverride_Rejects_Overlapping_Window_For_Same_Tenant_And_Feature()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        FeatureDefinition feature = await CreateFeatureAsync(scope);

        ITenantFeatureOverrideRepository overrides = scope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        TenantFeatureOverride first = TenantFeatureOverride.Create(
            tenantId, feature, true, EffectiveFrom, EffectiveFrom.AddMonths(3), "First override", Guid.NewGuid(), EffectiveFrom);
        overrides.Add(first);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope secondScope = _factory.Services.CreateScope();
        ITenantFeatureOverrideRepository secondOverrides = secondScope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork secondUnitOfWork = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        TenantFeatureOverride overlapping = TenantFeatureOverride.Create(
            tenantId, feature, false, EffectiveFrom.AddMonths(1), null, "Overlapping override", Guid.NewGuid(), EffectiveFrom);
        secondOverrides.Add(overlapping);

        Func<Task> act = () => secondUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task TenantFeatureOverride_Allows_A_New_Override_After_The_Prior_Window_Closes()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        FeatureDefinition feature = await CreateFeatureAsync(scope);

        ITenantFeatureOverrideRepository overrides = scope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        TenantFeatureOverride first = TenantFeatureOverride.Create(
            tenantId, feature, true, EffectiveFrom, EffectiveFrom.AddMonths(3), "First override", Guid.NewGuid(), EffectiveFrom);
        overrides.Add(first);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope secondScope = _factory.Services.CreateScope();
        ITenantFeatureOverrideRepository secondOverrides = secondScope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork secondUnitOfWork = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        TenantFeatureOverride afterward = TenantFeatureOverride.Create(
            tenantId, feature, false, EffectiveFrom.AddMonths(3), null, "Follow-up override", Guid.NewGuid(), EffectiveFrom);
        secondOverrides.Add(afterward);

        Func<Task> act = () => secondUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TenantFeatureOverride_HasOverlappingOverrideAsync_PreCheck_Detects_Overlap()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        FeatureDefinition feature = await CreateFeatureAsync(scope);

        ITenantFeatureOverrideRepository overrides = scope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        TenantFeatureOverride first = TenantFeatureOverride.Create(
            tenantId, feature, true, EffectiveFrom, EffectiveFrom.AddMonths(3), "First override", Guid.NewGuid(), EffectiveFrom);
        overrides.Add(first);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        bool hasOverlap = await readScope.ServiceProvider
            .GetRequiredService<ITenantFeatureOverrideRepository>()
            .HasOverlappingOverrideAsync(tenantId, feature.Id, EffectiveFrom.AddMonths(1), null, excludeId: null, CancellationToken.None);

        hasOverlap.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_Returns_A_Tracked_Override_That_Update_Persists()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        FeatureDefinition feature = await CreateFeatureAsync(scope);

        ITenantFeatureOverrideRepository overrides = scope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        TenantFeatureOverride @override = TenantFeatureOverride.Create(
            tenantId, feature, true, EffectiveFrom, null, "Pilot enterprise customer", Guid.NewGuid(), EffectiveFrom);
        overrides.Add(@override);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope editScope = _factory.Services.CreateScope();
        ITenantFeatureOverrideRepository editOverrides = editScope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork editUnitOfWork = editScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        TenantFeatureOverride tracked = await editOverrides.GetByIdAsync(@override.Id, CancellationToken.None)
            ?? throw new InvalidOperationException("Override was not found for edit.");
        tracked.Update(feature, false, EffectiveFrom.AddDays(1), EffectiveFrom.AddMonths(1), "Revised reason");
        await editUnitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        List<TenantFeatureOverride> all = await readScope.ServiceProvider
            .GetRequiredService<ITenantFeatureOverrideRepository>()
            .GetForTenantAsync(tenantId, CancellationToken.None);

        TenantFeatureOverride reloaded = all.Should().ContainSingle(o => o.Id == @override.Id).Subject;
        reloaded.Enabled.Should().BeFalse();
        reloaded.EffectiveFrom.Should().Be(EffectiveFrom.AddDays(1));
        reloaded.EffectiveUntil.Should().Be(EffectiveFrom.AddMonths(1));
        reloaded.Reason.Should().Be("Revised reason");
    }

    [Fact]
    public async Task End_Persists_A_Shortened_Window()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        FeatureDefinition feature = await CreateFeatureAsync(scope);

        ITenantFeatureOverrideRepository overrides = scope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid tenantId = Guid.NewGuid();
        TenantFeatureOverride @override = TenantFeatureOverride.Create(
            tenantId, feature, true, EffectiveFrom, null, "Temporary support exception", Guid.NewGuid(), EffectiveFrom);
        overrides.Add(@override);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope editScope = _factory.Services.CreateScope();
        ITenantFeatureOverrideRepository editOverrides = editScope.ServiceProvider.GetRequiredService<ITenantFeatureOverrideRepository>();
        IUnitOfWork editUnitOfWork = editScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        TenantFeatureOverride tracked = await editOverrides.GetByIdAsync(@override.Id, CancellationToken.None)
            ?? throw new InvalidOperationException("Override was not found for edit.");
        tracked.End(EffectiveFrom.AddMonths(1));
        await editUnitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        List<TenantFeatureOverride> all = await readScope.ServiceProvider
            .GetRequiredService<ITenantFeatureOverrideRepository>()
            .GetForTenantAsync(tenantId, CancellationToken.None);

        TenantFeatureOverride reloaded = all.Should().ContainSingle(o => o.Id == @override.Id).Subject;
        reloaded.EffectiveUntil.Should().Be(EffectiveFrom.AddMonths(1));
    }
}
