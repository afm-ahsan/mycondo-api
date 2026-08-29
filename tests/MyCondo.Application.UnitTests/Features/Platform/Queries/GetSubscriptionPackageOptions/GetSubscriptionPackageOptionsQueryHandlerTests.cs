using AwesomeAssertions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetSubscriptionPackageOptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.GetSubscriptionPackageOptions;

public class GetSubscriptionPackageOptionsQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 29, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(NowUtc.UtcDateTime);

    private readonly ISubscriptionPackageRepository _subscriptionPackages = Substitute.For<ISubscriptionPackageRepository>();
    private readonly ISubscriptionPackageVersionRepository _subscriptionPackageVersions =
        Substitute.For<ISubscriptionPackageVersionRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetSubscriptionPackageOptionsQueryHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private GetSubscriptionPackageOptionsQueryHandler CreateHandler() =>
        new(_subscriptionPackages, _subscriptionPackageVersions, _clock);

    private static (SubscriptionPackage Package, SubscriptionPackageVersion Version) CreateActivePackageAndCurrentVersion(
        string code = "PRO",
        string name = "Professional",
        decimal? monthlyPrice = 1500m,
        decimal? quarterlyPrice = null,
        decimal? semiAnnualPrice = null,
        decimal? annualPrice = 15000m,
        DateOnly? effectiveFrom = null,
        DateOnly? effectiveUntil = null,
        string currency = "BDT")
    {
        SubscriptionPackage package = SubscriptionPackage.Create(code, name, description: null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id,
            version: 1,
            effectiveFrom: effectiveFrom ?? Today.AddDays(-30),
            effectiveUntil: effectiveUntil,
            monthlyPrice: monthlyPrice,
            quarterlyPrice: quarterlyPrice,
            semiAnnualPrice: semiAnnualPrice,
            annualPrice: annualPrice,
            currency: currency);
        version.Activate();
        package.Activate();
        package.SetCurrentVersion(version.Id);
        return (package, version);
    }

    [Fact]
    public async Task Returns_Commercially_Assignable_Package_With_Its_Priced_Billing_Cycles()
    {
        (SubscriptionPackage package, SubscriptionPackageVersion version) = CreateActivePackageAndCurrentVersion();
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([package]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([version]);

        List<SubscriptionPackageOptionDto> result =
            await CreateHandler().Handle(new GetSubscriptionPackageOptionsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        SubscriptionPackageOptionDto option = result[0];
        option.PackageId.Should().Be(package.Id.Value);
        option.PackageCode.Should().Be("PRO");
        option.PackageName.Should().Be("Professional");
        option.PackageVersionId.Should().Be(version.Id.Value);
        option.Version.Should().Be(1);
        option.Currency.Should().Be("BDT");
        option.BillingCycles.Should().BeEquivalentTo(
        [
            new SubscriptionPackageBillingCycleOptionDto(nameof(BillingCycle.Monthly), 1500m),
            new SubscriptionPackageBillingCycleOptionDto(nameof(BillingCycle.Annual), 15000m),
        ]);
    }

    [Fact]
    public async Task Excludes_Inactive_Package()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("DRAFT", "Draft Package", description: null);
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([package]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        List<SubscriptionPackageOptionDto> result =
            await CreateHandler().Handle(new GetSubscriptionPackageOptionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Excludes_Package_Whose_Current_Version_Is_Not_Active()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("PRO", "Professional", description: null);
        SubscriptionPackageVersion draftVersion = SubscriptionPackageVersion.Create(
            package.Id, version: 1, effectiveFrom: Today.AddDays(-30), effectiveUntil: null,
            monthlyPrice: 1500m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null, currency: "BDT");
        package.Activate();
        // Package points at a version that never became Active (still Draft) — must not be offered.
        package.SetCurrentVersion(draftVersion.Id);

        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([package]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([draftVersion]);

        List<SubscriptionPackageOptionDto> result =
            await CreateHandler().Handle(new GetSubscriptionPackageOptionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Excludes_Version_Not_Yet_Effective()
    {
        (SubscriptionPackage package, SubscriptionPackageVersion version) =
            CreateActivePackageAndCurrentVersion(effectiveFrom: Today.AddDays(30));
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([package]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([version]);

        List<SubscriptionPackageOptionDto> result =
            await CreateHandler().Handle(new GetSubscriptionPackageOptionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Excludes_Expired_Version()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("PRO", "Professional", description: null);
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            package.Id, version: 1, effectiveFrom: Today.AddDays(-60), effectiveUntil: Today.AddDays(-1),
            monthlyPrice: 1500m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null, currency: "BDT");
        version.Activate();
        package.Activate();
        package.SetCurrentVersion(version.Id);
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([package]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([version]);

        List<SubscriptionPackageOptionDto> result =
            await CreateHandler().Handle(new GetSubscriptionPackageOptionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Excludes_Legacy_Migration_Grandfathered_Package()
    {
        (SubscriptionPackage package, SubscriptionPackageVersion version) =
            CreateActivePackageAndCurrentVersion(code: "LEGACY-MIGRATION-GRANDFATHERED", name: "Legacy Migration Bridge");
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([package]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([version]);

        List<SubscriptionPackageOptionDto> result =
            await CreateHandler().Handle(new GetSubscriptionPackageOptionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Excludes_Version_With_No_Priced_Billing_Cycle()
    {
        (SubscriptionPackage package, SubscriptionPackageVersion version) = CreateActivePackageAndCurrentVersion(
            monthlyPrice: null, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null);
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([package]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([version]);

        List<SubscriptionPackageOptionDto> result =
            await CreateHandler().Handle(new GetSubscriptionPackageOptionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_Multiple_Eligible_Packages_Ordered_By_Name()
    {
        (SubscriptionPackage packageB, SubscriptionPackageVersion versionB) =
            CreateActivePackageAndCurrentVersion(code: "BASIC", name: "Basic");
        (SubscriptionPackage packageA, SubscriptionPackageVersion versionA) =
            CreateActivePackageAndCurrentVersion(code: "ENT", name: "Enterprise");

        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([packageB, packageA]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([versionB, versionA]);

        List<SubscriptionPackageOptionDto> result =
            await CreateHandler().Handle(new GetSubscriptionPackageOptionsQuery(), CancellationToken.None);

        result.Select(o => o.PackageName).Should().Equal("Basic", "Enterprise");
    }
}
