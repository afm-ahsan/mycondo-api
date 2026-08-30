using AwesomeAssertions;
using MyCondo.Application.Features.Platform.Services.BillingLifecycleDecision;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;

namespace MyCondo.Application.UnitTests.Features.Platform.Services.BillingLifecycleDecision;

public class BillingLifecycleDecisionServiceTests
{
    private static readonly DateOnly Today = new(2026, 8, 30);
    private static readonly DateTimeOffset SomeUtc = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);

    private readonly BillingLifecycleDecisionService _sut = new();

    private static OrganizationSubscription CreateSubscription(
        Guid tenantId, OrganizationSubscriptionStatus status)
    {
        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenantId, SubscriptionPackageVersionId.New(), BillingCycle.Monthly,
            new DateOnly(2026, 1, 1), null, new DateOnly(2026, 9, 1),
            5000m, 0m, "BDT", SomeUtc, autoRenew: true);

        switch (status)
        {
            case OrganizationSubscriptionStatus.Active:
                break;
            case OrganizationSubscriptionStatus.PastDue:
                subscription.MarkPastDue();
                break;
            case OrganizationSubscriptionStatus.Restricted:
                subscription.MarkPastDue();
                subscription.Restrict(SomeUtc);
                break;
            case OrganizationSubscriptionStatus.Expired:
                subscription.MarkPastDue();
                subscription.Restrict(SomeUtc);
                subscription.Expire(SomeUtc);
                break;
            case OrganizationSubscriptionStatus.Canceled:
                subscription.Cancel(SomeUtc);
                break;
        }

        return subscription;
    }

    private static SubscriptionInvoice IssueInvoice(
        Guid tenantId, DateOnly dueDate, string currency = "BDT", decimal amount = 5000m)
    {
        (SubscriptionInvoice invoice, _) = SubscriptionInvoice.Issue(
            tenantId, OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}",
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), dueDate, currency,
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, amount, 0m, amount, "Professional (Monthly)")],
            SomeUtc);

        return invoice;
    }

    private static DateOnly DaysBeforeToday(int days) => Today.AddDays(-days);

    [Fact]
    public void Active_With_No_Overdue_Invoice_Recommends_NoAction()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Active);
        SubscriptionInvoice notDue = IssueInvoice(tenantId, Today.AddDays(5));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [notDue], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.NoAction);
        result.HasOverdueCollectibleInvoice.Should().BeFalse();
        result.MaxDaysOverdue.Should().Be(0);
    }

    [Fact]
    public void Active_With_One_Day_Overdue_Invoice_Recommends_MarkPastDue()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Active);
        SubscriptionInvoice overdue = IssueInvoice(tenantId, DaysBeforeToday(1));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [overdue], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.MarkPastDue);
        result.MaxDaysOverdue.Should().Be(1);
    }

    [Fact]
    public void PastDue_With_No_Overdue_Invoice_Recommends_Reactivate()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.PastDue);
        SubscriptionInvoice notDue = IssueInvoice(tenantId, Today.AddDays(1));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [notDue], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.Reactivate);
    }

    [Fact]
    public void PastDue_At_29_Days_Overdue_Recommends_NoAction()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.PastDue);
        SubscriptionInvoice overdue = IssueInvoice(tenantId, DaysBeforeToday(29));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [overdue], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.NoAction);
        result.MaxDaysOverdue.Should().Be(29);
    }

    [Fact]
    public void PastDue_At_30_Days_Overdue_Recommends_Restrict()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.PastDue);
        SubscriptionInvoice overdue = IssueInvoice(tenantId, DaysBeforeToday(30));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [overdue], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.Restrict);
        result.MaxDaysOverdue.Should().Be(30);
    }

    [Fact]
    public void Restricted_At_59_Days_Overdue_Recommends_NoAction()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Restricted);
        SubscriptionInvoice overdue = IssueInvoice(tenantId, DaysBeforeToday(59));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [overdue], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.NoAction);
    }

    [Fact]
    public void Restricted_At_60_Days_Overdue_Recommends_Expire()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Restricted);
        SubscriptionInvoice overdue = IssueInvoice(tenantId, DaysBeforeToday(60));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [overdue], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.Expire);
    }

    [Fact]
    public void Restricted_With_No_Overdue_Invoice_Never_Recommends_Reactivate()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Restricted);
        SubscriptionInvoice notDue = IssueInvoice(tenantId, Today.AddDays(1));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [notDue], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.NoAction);
    }

    [Fact]
    public void Expired_Always_Recommends_NoAction()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Expired);
        SubscriptionInvoice overdue = IssueInvoice(tenantId, DaysBeforeToday(150));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [overdue], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.NoAction);
    }

    [Fact]
    public void Canceled_Always_Recommends_NoAction()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Canceled);
        SubscriptionInvoice overdue = IssueInvoice(tenantId, DaysBeforeToday(150));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [overdue], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.NoAction);
    }

    [Fact]
    public void Paid_Invoices_Are_Ignored()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Active);
        SubscriptionInvoice paid = IssueInvoice(tenantId, DaysBeforeToday(10));
        paid.MarkPaid(SomeUtc);

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [paid], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.NoAction);
        result.HasOverdueCollectibleInvoice.Should().BeFalse();
    }

    [Fact]
    public void Void_And_Canceled_Invoices_Are_Ignored()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Active);
        SubscriptionInvoice voided = IssueInvoice(tenantId, DaysBeforeToday(10));
        voided.Void("billed in error", SomeUtc);
        SubscriptionInvoice canceled = IssueInvoice(tenantId, DaysBeforeToday(10));
        canceled.Cancel("commercial waiver", SomeUtc);

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [voided, canceled], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.NoAction);
        result.HasOverdueCollectibleInvoice.Should().BeFalse();
    }

    [Fact]
    public void Partially_Paid_Invoice_With_Remaining_Outstanding_Still_Counts_As_Collectible()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Active);
        SubscriptionInvoice partiallyPaid = IssueInvoice(tenantId, DaysBeforeToday(10), amount: 5000m);
        partiallyPaid.ApplyPayment(3000m, "BDT", SomeUtc);

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [partiallyPaid], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.MarkPastDue);
        result.HasOverdueCollectibleInvoice.Should().BeTrue();
    }

    [Fact]
    public void Partial_Payment_Does_Not_Reset_DaysOverdue()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.PastDue);
        SubscriptionInvoice partiallyPaid = IssueInvoice(tenantId, DaysBeforeToday(15), amount: 5000m);
        partiallyPaid.ApplyPayment(1000m, "BDT", SomeUtc);

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [partiallyPaid], Today);

        result.MaxDaysOverdue.Should().Be(15);
    }

    [Fact]
    public void DueDate_Drives_Ageing_Not_BillingPeriodEnd()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Active);

        // BillingPeriodEnd (2026-03-31) is far in the past, but DueDate is only 3 days overdue.
        SubscriptionInvoice invoice = IssueInvoice(tenantId, DaysBeforeToday(3));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [invoice], Today);

        result.MaxDaysOverdue.Should().Be(3);
    }

    [Fact]
    public void Invoice_Due_Exactly_Today_Is_Not_Yet_Overdue()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Active);
        SubscriptionInvoice dueToday = IssueInvoice(tenantId, Today);

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [dueToday], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.NoAction);
        result.HasOverdueCollectibleInvoice.Should().BeFalse();
    }

    [Fact]
    public void Multiple_Currencies_Are_Not_Summed_And_Do_Not_Affect_The_DaysOverdue_Decision()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.PastDue);
        SubscriptionInvoice bdt = IssueInvoice(tenantId, DaysBeforeToday(10), currency: "BDT");
        SubscriptionInvoice usd = IssueInvoice(tenantId, DaysBeforeToday(35), currency: "USD");

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [bdt, usd], Today);

        result.RecommendedAction.Should().Be(BillingLifecycleRecommendedAction.Restrict);
        result.MaxDaysOverdue.Should().Be(35);
    }

    [Fact]
    public void Organization_Decision_Uses_The_Maximum_DaysOverdue_Across_Collectible_Invoices()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Active);
        SubscriptionInvoice lessOverdue = IssueInvoice(tenantId, DaysBeforeToday(1));
        SubscriptionInvoice moreOverdue = IssueInvoice(tenantId, DaysBeforeToday(20));

        BillingLifecycleDecisionResult result = _sut.Evaluate(subscription, [lessOverdue, moreOverdue], Today);

        result.MaxDaysOverdue.Should().Be(20);
    }

    [Fact]
    public void Evaluation_Does_Not_Mutate_The_Subscription_Status()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.Active);
        SubscriptionInvoice overdue = IssueInvoice(tenantId, DaysBeforeToday(90));

        _sut.Evaluate(subscription, [overdue], Today);

        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Active);
    }

    [Fact]
    public void Same_Inputs_Produce_The_Same_Result_Deterministically()
    {
        Guid tenantId = Guid.NewGuid();
        OrganizationSubscription subscription = CreateSubscription(tenantId, OrganizationSubscriptionStatus.PastDue);
        SubscriptionInvoice overdue = IssueInvoice(tenantId, DaysBeforeToday(45));

        BillingLifecycleDecisionResult first = _sut.Evaluate(subscription, [overdue], Today);
        BillingLifecycleDecisionResult second = _sut.Evaluate(subscription, [overdue], Today);

        first.Should().Be(second);
    }
}
