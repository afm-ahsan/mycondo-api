using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Domain.UnitTests.Features.Platform.OrganizationSubscriptions;

public class OrganizationSubscriptionCommercialTermsTests
{
    private static readonly SubscriptionPackageId PackageId = SubscriptionPackageId.New();
    private static readonly DateOnly EffectiveFrom = new(2026, 1, 1);

    private static SubscriptionPackageVersion CreateVersion(
        decimal? monthlyPrice = 8000m,
        decimal? quarterlyPrice = null,
        decimal? semiAnnualPrice = null,
        decimal? annualPrice = 80000m) =>
        SubscriptionPackageVersion.Create(
            PackageId, 1, EffectiveFrom, null, monthlyPrice, quarterlyPrice, semiAnnualPrice, annualPrice, "BDT");

    [Fact]
    public void ResolveBasePrice_Returns_MonthlyPrice_For_Monthly_Cycle()
    {
        SubscriptionPackageVersion version = CreateVersion(monthlyPrice: 8000m);

        decimal price = OrganizationSubscriptionCommercialTerms.ResolveBasePrice(version, BillingCycle.Monthly);

        price.Should().Be(8000m);
    }

    [Fact]
    public void ResolveBasePrice_Returns_AnnualPrice_For_Annual_Cycle()
    {
        SubscriptionPackageVersion version = CreateVersion(annualPrice: 80000m);

        decimal price = OrganizationSubscriptionCommercialTerms.ResolveBasePrice(version, BillingCycle.Annual);

        price.Should().Be(80000m);
    }

    [Fact]
    public void ResolveBasePrice_Throws_When_Cycle_Not_Offered_By_Package_Version()
    {
        SubscriptionPackageVersion version = CreateVersion(quarterlyPrice: null);

        Action act = () => OrganizationSubscriptionCommercialTerms.ResolveBasePrice(version, BillingCycle.Quarterly);

        act.Should().Throw<UnsupportedBillingCycleException>();
    }

    [Fact]
    public void ResolveBasePrice_Throws_When_SemiAnnual_Not_Offered_By_Package_Version()
    {
        SubscriptionPackageVersion version = CreateVersion(semiAnnualPrice: null);

        Action act = () => OrganizationSubscriptionCommercialTerms.ResolveBasePrice(version, BillingCycle.SemiAnnual);

        act.Should().Throw<UnsupportedBillingCycleException>();
    }
}
