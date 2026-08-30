using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Authorization;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory) for the
/// Feature Catalogue foundation (ADR-033 Task 01) — no RLS on <c>platform.feature_definitions</c>/
/// <c>platform.feature_permissions</c> (platform-schema, not tenant data), so unlike RoleEndpointsDbTests
/// these need no tenant seeding. These need a Docker daemon and were NOT executed in the environment they
/// were authored in — see PostgresApiFactory's doc comment. Run wherever Docker is available before
/// trusting them.
/// </summary>
public class FeatureCatalogueDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public FeatureCatalogueDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FeatureDefinition_Persists_And_Round_Trips_Parent_Child()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IFeatureDefinitionRepository repository = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        string suffix = Guid.NewGuid().ToString("N")[..8];
        FeatureDefinition root = FeatureDefinition.Create(
            $"test.{suffix}", "Test Root", null, null, "test", FeatureCatalogueStatus.Active, false, 5000, FeatureEntitlementType.Boolean);
        FeatureDefinition child = FeatureDefinition.Create(
            $"test.{suffix}.child", "Test Child", "a child feature", root.Id, "test", FeatureCatalogueStatus.Active, false, 5001, FeatureEntitlementType.Boolean);

        repository.Add(root);
        repository.Add(child);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        List<FeatureDefinition> all = await readScope.ServiceProvider
            .GetRequiredService<IFeatureDefinitionRepository>()
            .GetAllAsync(CancellationToken.None);

        FeatureDefinition reloadedChild = all.Should().ContainSingle(f => f.Key == $"test.{suffix}.child").Subject;
        reloadedChild.ParentFeatureId.Should().Be(root.Id);
        reloadedChild.Name.Should().Be("Test Child");
        reloadedChild.Description.Should().Be("a child feature");
    }

    [Fact]
    public async Task FeatureDefinition_Rejects_Duplicate_Key()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IFeatureDefinitionRepository repository = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        string key = $"test.dup.{Guid.NewGuid():N}";
        repository.Add(FeatureDefinition.Create(
            key, "First", null, null, "test", FeatureCatalogueStatus.Active, false, 5000, FeatureEntitlementType.Boolean));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope duplicateScope = _factory.Services.CreateScope();
        IFeatureDefinitionRepository duplicateRepository = duplicateScope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IUnitOfWork duplicateUnitOfWork = duplicateScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        duplicateRepository.Add(FeatureDefinition.Create(
            key, "Second", null, null, "test", FeatureCatalogueStatus.Active, false, 5001, FeatureEntitlementType.Boolean));

        Func<Task> act = () => duplicateUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task FeaturePermission_Persists_And_Round_Trips()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IFeatureDefinitionRepository featureDefinitions = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        IFeaturePermissionRepository featurePermissions = scope.ServiceProvider.GetRequiredService<IFeaturePermissionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        FeatureDefinition feature = FeatureDefinition.Create(
            $"test.mapping.{Guid.NewGuid():N}", "Test Mapping Feature", null, null, "test",
            FeatureCatalogueStatus.Active, false, 5000, FeatureEntitlementType.Boolean);
        featureDefinitions.Add(feature);
        featurePermissions.Add(new FeaturePermission(feature.Id, "expense.view"));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        List<FeaturePermission> mappings = await readScope.ServiceProvider
            .GetRequiredService<IFeaturePermissionRepository>()
            .GetAllAsync(CancellationToken.None);

        mappings.Should().ContainSingle(m => m.FeatureId == feature.Id && m.PermissionCode == "expense.view");
    }

    [Fact]
    public async Task FeatureCatalogueSeeder_Seeds_The_Full_Catalogue_Idempotently()
    {
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IFeatureCatalogueSeeder>().SeedAsync(CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        }

        using IServiceScope readScope = _factory.Services.CreateScope();
        List<FeatureDefinition> definitions = await readScope.ServiceProvider
            .GetRequiredService<IFeatureDefinitionRepository>().GetAllAsync(CancellationToken.None);
        List<FeaturePermission> mappings = await readScope.ServiceProvider
            .GetRequiredService<IFeaturePermissionRepository>().GetAllAsync(CancellationToken.None);

        definitions.Select(d => d.Key).Should().Contain(FeatureCatalogue.Entries.Select(e => e.Key));
        mappings.Should().HaveCountGreaterThanOrEqualTo(FeatureCatalogue.PermissionMappings.Length);

        // Second run must not duplicate anything — reconciliation is additive-only by natural key.
        using (IServiceScope secondScope = _factory.Services.CreateScope())
        {
            await secondScope.ServiceProvider.GetRequiredService<IFeatureCatalogueSeeder>().SeedAsync(CancellationToken.None);
            await secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        }

        using IServiceScope finalScope = _factory.Services.CreateScope();
        List<FeatureDefinition> definitionsAfterSecondRun = await finalScope.ServiceProvider
            .GetRequiredService<IFeatureDefinitionRepository>().GetAllAsync(CancellationToken.None);
        definitionsAfterSecondRun.Should().HaveCount(definitions.Count);
    }
}
