using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.FeatureCatalogue;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Round-trip tests against a real, ephemeral PostgreSQL container (see PostgresApiFactory) for the
/// Subscription Package foundation (ADR-033 Task 02) — no RLS on the <c>platform.subscription_*</c>
/// tables (platform-schema, not tenant data), same reasoning as FeatureCatalogueDbTests. These need a
/// Docker daemon and were NOT executed in the environment they were authored in — see PostgresApiFactory's
/// doc comment. Run wherever Docker is available before trusting them.
/// </summary>
public class SubscriptionPackageDbTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public SubscriptionPackageDbTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubscriptionPackage_Persists_And_Round_Trips()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository repository = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        string suffix = Guid.NewGuid().ToString("N")[..8];
        SubscriptionPackage package = SubscriptionPackage.Create($"test-{suffix}", "Test Package", "a test package");
        package.Activate();

        repository.Add(package);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        List<SubscriptionPackage> all = await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionPackageRepository>()
            .GetAllAsync(CancellationToken.None);

        SubscriptionPackage reloaded = all.Should().ContainSingle(p => p.Code == $"test-{suffix}").Subject;
        reloaded.Name.Should().Be("Test Package");
        reloaded.Status.Should().Be(SubscriptionPackageStatus.Active);
    }

    [Fact]
    public async Task SubscriptionPackage_Rejects_Duplicate_Code()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository repository = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        string code = $"test-dup-{Guid.NewGuid():N}";
        repository.Add(SubscriptionPackage.Create(code, "First", null));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope duplicateScope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository duplicateRepository = duplicateScope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        IUnitOfWork duplicateUnitOfWork = duplicateScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        duplicateRepository.Add(SubscriptionPackage.Create(code, "Second", null));

        Func<Task> act = () => duplicateUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SubscriptionPackageVersion_Persists_And_Round_Trips_Prices_And_Precision()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{Guid.NewGuid():N}", "Professional", null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, new DateOnly(2026, 1, 1), null, 8000.50m, 22000m, null, 80000m, "BDT");

        packages.Add(package);
        versions.Add(version);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        List<SubscriptionPackageVersion> all = await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionPackageVersionRepository>()
            .GetAllAsync(CancellationToken.None);

        SubscriptionPackageVersion reloaded = all.Should().ContainSingle(v => v.Id == version.Id).Subject;
        reloaded.PackageId.Should().Be(package.Id);
        reloaded.MonthlyPrice.Should().Be(8000.50m);
        reloaded.QuarterlyPrice.Should().Be(22000m);
        reloaded.SemiAnnualPrice.Should().BeNull();
        reloaded.AnnualPrice.Should().Be(80000m);
        reloaded.Currency.Should().Be("BDT");
    }

    [Fact]
    public async Task SubscriptionPackageVersion_Rejects_Duplicate_Version_Number_For_Same_Package()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{Guid.NewGuid():N}", "Professional", null);
        packages.Add(package);
        versions.Add(SubscriptionPackageVersion.Create(package.Id, 1, new DateOnly(2026, 1, 1), null, 8000m, null, null, null, "BDT"));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope duplicateScope = _factory.Services.CreateScope();
        ISubscriptionPackageVersionRepository duplicateVersions = duplicateScope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork duplicateUnitOfWork = duplicateScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        duplicateVersions.Add(SubscriptionPackageVersion.Create(package.Id, 1, new DateOnly(2026, 2, 1), null, 9000m, null, null, null, "BDT"));

        Func<Task> act = () => duplicateUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SubscriptionPackageVersion_Rejects_A_Second_Active_Version_For_The_Same_Package()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{Guid.NewGuid():N}", "Professional", null);
        packages.Add(package);

        SubscriptionPackageVersion firstVersion = SubscriptionPackageVersion.Create(
            package.Id, 1, new DateOnly(2026, 1, 1), null, 8000m, null, null, null, "BDT");
        firstVersion.Activate();
        versions.Add(firstVersion);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope secondScope = _factory.Services.CreateScope();
        ISubscriptionPackageVersionRepository secondVersions = secondScope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IUnitOfWork secondUnitOfWork = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackageVersion secondVersion = SubscriptionPackageVersion.Create(
            package.Id, 2, new DateOnly(2026, 6, 1), null, 9000m, null, null, null, "BDT");
        secondVersion.Activate();
        secondVersions.Add(secondVersion);

        Func<Task> act = () => secondUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SubscriptionPackageFeature_Persists_And_Round_Trips()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IFeatureDefinitionRepository features = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        ISubscriptionPackageFeatureRepository packageFeatures = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageFeatureRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{Guid.NewGuid():N}", "Professional", null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, new DateOnly(2026, 1, 1), null, 8000m, null, null, null, "BDT");
        FeatureDefinition feature = FeatureDefinition.Create(
            $"test.{Guid.NewGuid():N}", "Test Feature", null, null, "test", FeatureCatalogueStatus.Active, false, 5000, FeatureEntitlementType.Boolean);

        packages.Add(package);
        versions.Add(version);
        features.Add(feature);
        packageFeatures.Add(new SubscriptionPackageFeature(version.Id, feature.Id, enabled: true, limitValue: null));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope readScope = _factory.Services.CreateScope();
        List<SubscriptionPackageFeature> all = await readScope.ServiceProvider
            .GetRequiredService<ISubscriptionPackageFeatureRepository>()
            .GetAllAsync(CancellationToken.None);

        all.Should().ContainSingle(f => f.PackageVersionId == version.Id && f.FeatureId == feature.Id && f.Enabled);
    }

    [Fact]
    public async Task SubscriptionPackageFeature_Rejects_Duplicate_Version_Feature_Mapping()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ISubscriptionPackageRepository packages = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageRepository>();
        ISubscriptionPackageVersionRepository versions = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageVersionRepository>();
        IFeatureDefinitionRepository features = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionRepository>();
        ISubscriptionPackageFeatureRepository packageFeatures = scope.ServiceProvider.GetRequiredService<ISubscriptionPackageFeatureRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        SubscriptionPackage package = SubscriptionPackage.Create($"test-{Guid.NewGuid():N}", "Professional", null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, 1, new DateOnly(2026, 1, 1), null, 8000m, null, null, null, "BDT");
        FeatureDefinition feature = FeatureDefinition.Create(
            $"test.{Guid.NewGuid():N}", "Test Feature", null, null, "test", FeatureCatalogueStatus.Active, false, 5000, FeatureEntitlementType.Boolean);

        packages.Add(package);
        versions.Add(version);
        features.Add(feature);
        packageFeatures.Add(new SubscriptionPackageFeature(version.Id, feature.Id, enabled: true, limitValue: null));
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        using IServiceScope duplicateScope = _factory.Services.CreateScope();
        ISubscriptionPackageFeatureRepository duplicatePackageFeatures = duplicateScope.ServiceProvider.GetRequiredService<ISubscriptionPackageFeatureRepository>();
        IUnitOfWork duplicateUnitOfWork = duplicateScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        duplicatePackageFeatures.Add(new SubscriptionPackageFeature(version.Id, feature.Id, enabled: false, limitValue: null));

        Func<Task> act = () => duplicateUnitOfWork.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
