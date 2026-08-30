using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Domain.UnitTests.Features.Platform.OrganizationSubscriptions;

public class OrganizationSubscriptionTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly SubscriptionPackageVersionId PackageVersionId = SubscriptionPackageVersionId.New();
    private static readonly DateOnly StartDate = new(2026, 1, 1);
    private static readonly DateTimeOffset ActivatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static OrganizationSubscription CreateSubscription(
        decimal basePrice = 8000m,
        decimal discount = 0m,
        string currency = "BDT",
        DateOnly? endDate = null,
        bool autoRenew = false) =>
        OrganizationSubscription.Create(
            TenantId, PackageVersionId, BillingCycle.Monthly, StartDate, endDate, null,
            basePrice, discount, currency, ActivatedAt, autoRenew);

    [Fact]
    public void Create_Sets_All_Fields_And_Starts_Active()
    {
        OrganizationSubscription subscription = OrganizationSubscription.Create(
            TenantId, PackageVersionId, BillingCycle.Annual, StartDate, null, StartDate.AddYears(1),
            80000m, 5000m, "BDT", ActivatedAt, autoRenew: true);

        subscription.TenantId.Should().Be(TenantId);
        subscription.PackageVersionId.Should().Be(PackageVersionId);
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Active);
        subscription.BillingCycle.Should().Be(BillingCycle.Annual);
        subscription.StartDate.Should().Be(StartDate);
        subscription.NextBillingDate.Should().Be(StartDate.AddYears(1));
        subscription.BasePrice.Should().Be(80000m);
        subscription.Discount.Should().Be(5000m);
        subscription.EffectivePrice.Should().Be(75000m);
        subscription.Currency.Should().Be("BDT");
        subscription.ActivatedAt.Should().Be(ActivatedAt);
        subscription.AutoRenew.Should().BeTrue();
    }

    [Fact]
    public void Create_Throws_For_Empty_TenantId()
    {
        Action act = () => OrganizationSubscription.Create(
            Guid.Empty, PackageVersionId, BillingCycle.Monthly, StartDate, null, null, 8000m, 0m, "BDT", ActivatedAt, false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_For_Negative_BasePrice()
    {
        Action act = () => CreateSubscription(basePrice: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Throws_For_Negative_Discount()
    {
        Action act = () => CreateSubscription(discount: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_Throws_When_Discount_Exceeds_BasePrice()
    {
        Action act = () => CreateSubscription(basePrice: 8000m, discount: 8001m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Computes_EffectivePrice_As_BasePrice_Minus_Discount()
    {
        OrganizationSubscription subscription = CreateSubscription(basePrice: 8000m, discount: 1500m);

        subscription.EffectivePrice.Should().Be(6500m);
    }

    [Fact]
    public void Create_Throws_For_Blank_Currency()
    {
        Action act = () => CreateSubscription(currency: "  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_When_EndDate_Before_StartDate()
    {
        Action act = () => CreateSubscription(endDate: StartDate.AddDays(-1));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkPastDue_From_Active_Transitions_To_PastDue()
    {
        OrganizationSubscription subscription = CreateSubscription();

        subscription.MarkPastDue();

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.PastDue);
    }

    [Fact]
    public void MarkPastDue_From_NonActive_Throws()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.MarkPastDue();

        Action act = () => subscription.MarkPastDue();

        act.Should().Throw<OrganizationSubscriptionInvalidTransitionException>();
    }

    [Fact]
    public void Reactivate_From_PastDue_Transitions_To_Active()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.MarkPastDue();

        subscription.Reactivate();

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Active);
    }

    [Fact]
    public void Reactivate_From_Active_Throws()
    {
        OrganizationSubscription subscription = CreateSubscription();

        Action act = () => subscription.Reactivate();

        act.Should().Throw<OrganizationSubscriptionInvalidTransitionException>();
    }

    [Fact]
    public void Restrict_From_PastDue_Transitions_To_Restricted_And_Stamps_RestrictedAt()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.MarkPastDue();
        DateTimeOffset restrictedAt = ActivatedAt.AddDays(30);

        subscription.Restrict(restrictedAt);

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Restricted);
        subscription.RestrictedAt.Should().Be(restrictedAt);
    }

    [Fact]
    public void Restrict_From_Active_Throws()
    {
        OrganizationSubscription subscription = CreateSubscription();

        Action act = () => subscription.Restrict(ActivatedAt.AddDays(1));

        act.Should().Throw<OrganizationSubscriptionInvalidTransitionException>();
    }

    [Fact]
    public void Cancel_From_Active_Transitions_To_Canceled_And_Stamps_CanceledAt()
    {
        OrganizationSubscription subscription = CreateSubscription();
        DateTimeOffset canceledAt = ActivatedAt.AddDays(10);

        subscription.Cancel(canceledAt);

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Canceled);
        subscription.CanceledAt.Should().Be(canceledAt);
    }

    [Fact]
    public void Cancel_From_PastDue_Transitions_To_Canceled()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.MarkPastDue();

        subscription.Cancel(ActivatedAt.AddDays(10));

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Canceled);
    }

    [Fact]
    public void Cancel_From_Restricted_Throws()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.MarkPastDue();
        subscription.Restrict(ActivatedAt.AddDays(30));

        Action act = () => subscription.Cancel(ActivatedAt.AddDays(31));

        act.Should().Throw<OrganizationSubscriptionInvalidTransitionException>();
    }

    [Fact]
    public void Expire_From_Restricted_Transitions_To_Expired_Terminal_State()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.MarkPastDue();
        subscription.Restrict(ActivatedAt.AddDays(30));
        DateTimeOffset expiredAt = ActivatedAt.AddDays(60);

        subscription.Expire(expiredAt);

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Expired);
        subscription.ExpiredAt.Should().Be(expiredAt);
    }

    [Fact]
    public void Expire_From_Canceled_Transitions_To_Expired()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.Cancel(ActivatedAt.AddDays(10));

        subscription.Expire(ActivatedAt.AddDays(40));

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Expired);
    }

    [Fact]
    public void Expire_From_Active_Throws()
    {
        OrganizationSubscription subscription = CreateSubscription();

        Action act = () => subscription.Expire(ActivatedAt.AddDays(1));

        act.Should().Throw<OrganizationSubscriptionInvalidTransitionException>();
    }

    [Fact]
    public void Expired_Subscription_Has_No_Further_Valid_Transitions()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.Cancel(ActivatedAt.AddDays(10));
        subscription.Expire(ActivatedAt.AddDays(40));

        subscription.Invoking(s => s.MarkPastDue()).Should().Throw<OrganizationSubscriptionInvalidTransitionException>();
        subscription.Invoking(s => s.Cancel(ActivatedAt.AddDays(50))).Should().Throw<OrganizationSubscriptionInvalidTransitionException>();
        subscription.Invoking(s => s.Expire(ActivatedAt.AddDays(50))).Should().Throw<OrganizationSubscriptionInvalidTransitionException>();
    }

    [Fact]
    public void Historical_Commercial_Fields_Do_Not_Mutate_Across_Lifecycle_Transitions()
    {
        OrganizationSubscription subscription = CreateSubscription(basePrice: 8000m, discount: 1000m);
        subscription.MarkPastDue();
        subscription.Restrict(ActivatedAt.AddDays(30));
        subscription.Expire(ActivatedAt.AddDays(60));

        subscription.BasePrice.Should().Be(8000m);
        subscription.Discount.Should().Be(1000m);
        subscription.EffectivePrice.Should().Be(7000m);
        subscription.Currency.Should().Be("BDT");
    }

    [Fact]
    public void Two_Subscriptions_With_Different_Ids_Are_Not_Equal()
    {
        OrganizationSubscription a = CreateSubscription();
        OrganizationSubscription b = CreateSubscription();

        a.Should().NotBe(b);
    }

    [Fact]
    public void ChangePackageVersion_From_Active_Updates_Package_Cycle_Price_Currency_And_AutoRenew()
    {
        OrganizationSubscription subscription = CreateSubscription(basePrice: 8000m, discount: 1000m, autoRenew: false);
        SubscriptionPackageVersionId newVersionId = SubscriptionPackageVersionId.New();

        subscription.ChangePackageVersion(newVersionId, BillingCycle.Annual, 96000m, "USD", autoRenew: true);

        subscription.PackageVersionId.Should().Be(newVersionId);
        subscription.BillingCycle.Should().Be(BillingCycle.Annual);
        subscription.BasePrice.Should().Be(96000m);
        subscription.Currency.Should().Be("USD");
        subscription.AutoRenew.Should().BeTrue();
    }

    [Fact]
    public void ChangePackageVersion_Resets_Discount_And_Recomputes_EffectivePrice()
    {
        OrganizationSubscription subscription = CreateSubscription(basePrice: 8000m, discount: 1500m);

        subscription.ChangePackageVersion(SubscriptionPackageVersionId.New(), BillingCycle.Monthly, 5000m, "BDT", autoRenew: false);

        subscription.Discount.Should().Be(0m);
        subscription.EffectivePrice.Should().Be(5000m);
    }

    [Fact]
    public void ChangePackageVersion_Does_Not_Alter_StartDate_Or_NextBillingDate()
    {
        OrganizationSubscription subscription = CreateSubscription();

        subscription.ChangePackageVersion(SubscriptionPackageVersionId.New(), BillingCycle.Annual, 96000m, "BDT", autoRenew: true);

        subscription.StartDate.Should().Be(StartDate);
        subscription.NextBillingDate.Should().BeNull();
    }

    [Fact]
    public void ChangePackageVersion_From_PastDue_Succeeds()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.MarkPastDue();

        subscription.ChangePackageVersion(SubscriptionPackageVersionId.New(), BillingCycle.Annual, 96000m, "BDT", autoRenew: true);

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.PastDue);
        subscription.BasePrice.Should().Be(96000m);
    }

    [Fact]
    public void ChangePackageVersion_From_Restricted_Throws()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.MarkPastDue();
        subscription.Restrict(ActivatedAt.AddDays(30));

        Action act = () => subscription.ChangePackageVersion(
            SubscriptionPackageVersionId.New(), BillingCycle.Annual, 96000m, "BDT", autoRenew: true);

        act.Should().Throw<OrganizationSubscriptionChangeNotAllowedException>();
    }

    [Fact]
    public void ChangePackageVersion_From_Canceled_Throws()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.Cancel(ActivatedAt.AddDays(10));

        Action act = () => subscription.ChangePackageVersion(
            SubscriptionPackageVersionId.New(), BillingCycle.Annual, 96000m, "BDT", autoRenew: true);

        act.Should().Throw<OrganizationSubscriptionChangeNotAllowedException>();
    }

    [Fact]
    public void ChangePackageVersion_From_Expired_Throws()
    {
        OrganizationSubscription subscription = CreateSubscription();
        subscription.Cancel(ActivatedAt.AddDays(10));
        subscription.Expire(ActivatedAt.AddDays(40));

        Action act = () => subscription.ChangePackageVersion(
            SubscriptionPackageVersionId.New(), BillingCycle.Annual, 96000m, "BDT", autoRenew: true);

        act.Should().Throw<OrganizationSubscriptionChangeNotAllowedException>();
    }

    [Fact]
    public void ChangePackageVersion_Throws_For_Negative_BasePrice()
    {
        OrganizationSubscription subscription = CreateSubscription();

        Action act = () => subscription.ChangePackageVersion(
            SubscriptionPackageVersionId.New(), BillingCycle.Monthly, -1m, "BDT", autoRenew: false);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ChangePackageVersion_Throws_For_Blank_Currency()
    {
        OrganizationSubscription subscription = CreateSubscription();

        Action act = () => subscription.ChangePackageVersion(
            SubscriptionPackageVersionId.New(), BillingCycle.Monthly, 5000m, "  ", autoRenew: false);

        act.Should().Throw<ArgumentException>();
    }
}
