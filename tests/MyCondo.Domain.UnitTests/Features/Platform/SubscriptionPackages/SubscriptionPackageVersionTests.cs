using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Platform.SubscriptionPackages;

public class SubscriptionPackageVersionTests
{
    private static readonly SubscriptionPackageId PackageId = SubscriptionPackageId.New();
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    private static SubscriptionPackageVersion CreateVersion(
        int version = 1,
        DateOnly? effectiveUntil = null,
        decimal? monthlyPrice = 8000m,
        string currency = "BDT") =>
        SubscriptionPackageVersion.Create(
            PackageId, version, EffectiveFrom, effectiveUntil, monthlyPrice, null, null, null, currency);

    [Fact]
    public void Create_Sets_All_Fields_And_Starts_Draft()
    {
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            PackageId, 1, EffectiveFrom, null, 8000m, 22000m, null, 80000m, "BDT");

        version.PackageId.Should().Be(PackageId);
        version.Version.Should().Be(1);
        version.EffectiveFrom.Should().Be(EffectiveFrom);
        version.EffectiveUntil.Should().BeNull();
        version.Status.Should().Be(SubscriptionPackageVersionStatus.Draft);
        version.MonthlyPrice.Should().Be(8000m);
        version.QuarterlyPrice.Should().Be(22000m);
        version.SemiAnnualPrice.Should().BeNull();
        version.AnnualPrice.Should().Be(80000m);
        version.Currency.Should().Be("BDT");
    }

    [Fact]
    public void Create_Throws_For_Non_Positive_Version()
    {
        Action act = () => CreateVersion(version: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Throws_When_EffectiveUntil_Before_EffectiveFrom()
    {
        Action act = () => CreateVersion(effectiveUntil: EffectiveFrom.AddDays(-1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Allows_EffectiveUntil_Equal_To_EffectiveFrom()
    {
        SubscriptionPackageVersion version = CreateVersion(effectiveUntil: EffectiveFrom);

        version.EffectiveUntil.Should().Be(EffectiveFrom);
    }

    [Fact]
    public void Create_Throws_For_Blank_Currency()
    {
        Action act = () => SubscriptionPackageVersion.Create(PackageId, 1, EffectiveFrom, null, 8000m, null, null, null, "  ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    public void Create_Throws_For_Negative_MonthlyPrice(decimal price)
    {
        Action act = () => SubscriptionPackageVersion.Create(PackageId, 1, EffectiveFrom, null, price, null, null, null, "BDT");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Allows_Null_Prices_For_Unsupported_Billing_Cycles()
    {
        SubscriptionPackageVersion version = SubscriptionPackageVersion.Create(
            PackageId, 1, EffectiveFrom, null, null, null, null, null, "BDT");

        version.MonthlyPrice.Should().BeNull();
        version.QuarterlyPrice.Should().BeNull();
        version.SemiAnnualPrice.Should().BeNull();
        version.AnnualPrice.Should().BeNull();
    }

    [Fact]
    public void Activate_Draft_Transitions_To_Active()
    {
        SubscriptionPackageVersion version = CreateVersion();

        version.Activate();

        version.Status.Should().Be(SubscriptionPackageVersionStatus.Active);
    }

    [Fact]
    public void Activate_Already_Active_Throws()
    {
        SubscriptionPackageVersion version = CreateVersion();
        version.Activate();

        Action act = () => version.Activate();

        act.Should().Throw<SubscriptionPackageVersionInvalidTransitionException>();
    }

    [Fact]
    public void Supersede_Active_Version_Closes_EffectiveUntil_And_Preserves_Price()
    {
        SubscriptionPackageVersion version = CreateVersion();
        version.Activate();
        DateOnly supersededOn = EffectiveFrom.AddMonths(6);

        version.Supersede(supersededOn);

        version.Status.Should().Be(SubscriptionPackageVersionStatus.Superseded);
        version.EffectiveUntil.Should().Be(supersededOn);
        version.MonthlyPrice.Should().Be(8000m);
    }

    [Fact]
    public void Supersede_Draft_Version_Throws()
    {
        SubscriptionPackageVersion version = CreateVersion();

        Action act = () => version.Supersede(EffectiveFrom.AddMonths(1));

        act.Should().Throw<SubscriptionPackageVersionInvalidTransitionException>();
    }

    [Fact]
    public void Supersede_Before_EffectiveFrom_Throws()
    {
        SubscriptionPackageVersion version = CreateVersion();
        version.Activate();

        Action act = () => version.Supersede(EffectiveFrom.AddDays(-1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Two_Versions_With_Different_Ids_Are_Not_Equal()
    {
        SubscriptionPackageVersion a = CreateVersion();
        SubscriptionPackageVersion b = CreateVersion();

        a.Should().NotBe(b);
    }
}
