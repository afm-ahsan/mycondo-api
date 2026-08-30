using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.ApplyOrganizationSubscriptionBillingDecision;
using MyCondo.Application.Features.Platform.Services.BillingLifecycleDecision;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.ApplyOrganizationSubscriptionBillingDecision;

/// <summary>
/// Exercises the handler against the real <see cref="BillingLifecycleDecisionService"/> (never a mock of
/// it) so these tests verify Task 14F actually consumes Task 14E's decision matrix rather than
/// reimplementing it.
/// </summary>
public class ApplyOrganizationSubscriptionBillingDecisionCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 8, 30);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IOrganizationSubscriptionRepository _subscriptions = Substitute.For<IOrganizationSubscriptionRepository>();
    private readonly ISubscriptionInvoiceRepository _invoices = Substitute.For<ISubscriptionInvoiceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ApplyOrganizationSubscriptionBillingDecisionCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private static Tenant CreateActiveTenant()
    {
        Tenant tenant = Tenant.Provision("ARP", "arp", NowUtc);
        tenant.Activate(NowUtc);
        return tenant;
    }

    private static OrganizationSubscription CreateSubscription(Guid tenantId, OrganizationSubscriptionStatus status)
    {
        OrganizationSubscription subscription = OrganizationSubscription.Create(
            tenantId, SubscriptionPackageVersionId.New(), BillingCycle.Monthly,
            new DateOnly(2026, 1, 1), null, new DateOnly(2026, 9, 1), 5000m, 0m, "BDT", NowUtc, autoRenew: true);

        switch (status)
        {
            case OrganizationSubscriptionStatus.Active:
                break;
            case OrganizationSubscriptionStatus.PastDue:
                subscription.MarkPastDue();
                break;
            case OrganizationSubscriptionStatus.Restricted:
                subscription.MarkPastDue();
                subscription.Restrict(NowUtc);
                break;
            case OrganizationSubscriptionStatus.Expired:
                subscription.MarkPastDue();
                subscription.Restrict(NowUtc);
                subscription.Expire(NowUtc);
                break;
            case OrganizationSubscriptionStatus.Canceled:
                subscription.Cancel(NowUtc);
                break;
        }

        return subscription;
    }

    private static SubscriptionInvoice IssueOverdueInvoice(Guid tenantId, int daysOverdue)
    {
        DateOnly dueDate = Today.AddDays(-daysOverdue);
        DateOnly billingPeriodStart = dueDate.AddDays(-30);
        (SubscriptionInvoice invoice, _) = SubscriptionInvoice.Issue(
            tenantId, OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}",
            billingPeriodStart, dueDate, billingPeriodStart, dueDate, "BDT",
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, 5000m, 0m, 5000m, "Professional (Monthly)")],
            NowUtc);

        return invoice;
    }

    private ApplyOrganizationSubscriptionBillingDecisionCommandHandler CreateHandler() => new(
        _tenants, _subscriptions, _invoices, new BillingLifecycleDecisionService(), _unitOfWork, _clock,
        Substitute.For<ILogger<ApplyOrganizationSubscriptionBillingDecisionCommandHandler>>());

    private void SeedTenantAndSubscription(Tenant tenant, OrganizationSubscription subscription)
    {
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _subscriptions.GetLatestForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(subscription);
    }

    private void SeedInvoices(Guid tenantId, params SubscriptionInvoice[] invoices) =>
        _invoices.GetOutstandingAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SubscriptionInvoice>)invoices);

    [Theory]
    [InlineData(1)]
    [InlineData(35)]
    [InlineData(60)]
    public async Task Active_With_Overdue_Invoice_Applies_Only_Active_To_PastDue(int daysOverdue)
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value, OrganizationSubscriptionStatus.Active);
        SeedTenantAndSubscription(tenant, subscription);
        SeedInvoices(tenant.Id.Value, IssueOverdueInvoice(tenant.Id.Value, daysOverdue));

        ApplyOrganizationSubscriptionBillingDecisionResult result = await CreateHandler().Handle(
            new ApplyOrganizationSubscriptionBillingDecisionCommand(tenant.Id.Value), CancellationToken.None);

        result.TransitionApplied.Should().BeTrue();
        result.PreviousStatus.Should().Be(OrganizationSubscriptionStatus.Active.ToString());
        result.ResultingStatus.Should().Be(OrganizationSubscriptionStatus.PastDue.ToString());
        result.Recommendation.Should().Be(BillingLifecycleRecommendedAction.MarkPastDue.ToString());
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.PastDue);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PastDue_With_No_Overdue_Invoice_Applies_Reactivate()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value, OrganizationSubscriptionStatus.PastDue);
        SeedTenantAndSubscription(tenant, subscription);
        SeedInvoices(tenant.Id.Value);

        ApplyOrganizationSubscriptionBillingDecisionResult result = await CreateHandler().Handle(
            new ApplyOrganizationSubscriptionBillingDecisionCommand(tenant.Id.Value), CancellationToken.None);

        result.TransitionApplied.Should().BeTrue();
        result.Recommendation.Should().Be(BillingLifecycleRecommendedAction.Reactivate.ToString());
        result.ResultingStatus.Should().Be(OrganizationSubscriptionStatus.Active.ToString());
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Active);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PastDue_Within_Grace_Window_Produces_NoAction_And_No_Mutation()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value, OrganizationSubscriptionStatus.PastDue);
        SeedTenantAndSubscription(tenant, subscription);
        SeedInvoices(tenant.Id.Value, IssueOverdueInvoice(tenant.Id.Value, 15));

        ApplyOrganizationSubscriptionBillingDecisionResult result = await CreateHandler().Handle(
            new ApplyOrganizationSubscriptionBillingDecisionCommand(tenant.Id.Value), CancellationToken.None);

        result.TransitionApplied.Should().BeFalse();
        result.Recommendation.Should().Be(BillingLifecycleRecommendedAction.NoAction.ToString());
        result.ResultingStatus.Should().Be(OrganizationSubscriptionStatus.PastDue.ToString());
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.PastDue);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PastDue_At_Least_30_Days_Overdue_Applies_Restrict()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value, OrganizationSubscriptionStatus.PastDue);
        SeedTenantAndSubscription(tenant, subscription);
        SeedInvoices(tenant.Id.Value, IssueOverdueInvoice(tenant.Id.Value, 30));

        ApplyOrganizationSubscriptionBillingDecisionResult result = await CreateHandler().Handle(
            new ApplyOrganizationSubscriptionBillingDecisionCommand(tenant.Id.Value), CancellationToken.None);

        result.TransitionApplied.Should().BeTrue();
        result.Recommendation.Should().Be(BillingLifecycleRecommendedAction.Restrict.ToString());
        result.ResultingStatus.Should().Be(OrganizationSubscriptionStatus.Restricted.ToString());
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Restricted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restricted_Below_60_Days_Overdue_Produces_NoAction_And_No_Mutation()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value, OrganizationSubscriptionStatus.Restricted);
        SeedTenantAndSubscription(tenant, subscription);
        SeedInvoices(tenant.Id.Value, IssueOverdueInvoice(tenant.Id.Value, 45));

        ApplyOrganizationSubscriptionBillingDecisionResult result = await CreateHandler().Handle(
            new ApplyOrganizationSubscriptionBillingDecisionCommand(tenant.Id.Value), CancellationToken.None);

        result.TransitionApplied.Should().BeFalse();
        result.Recommendation.Should().Be(BillingLifecycleRecommendedAction.NoAction.ToString());
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Restricted);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restricted_At_Least_60_Days_Overdue_Applies_Expire()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value, OrganizationSubscriptionStatus.Restricted);
        SeedTenantAndSubscription(tenant, subscription);
        SeedInvoices(tenant.Id.Value, IssueOverdueInvoice(tenant.Id.Value, 60));

        ApplyOrganizationSubscriptionBillingDecisionResult result = await CreateHandler().Handle(
            new ApplyOrganizationSubscriptionBillingDecisionCommand(tenant.Id.Value), CancellationToken.None);

        result.TransitionApplied.Should().BeTrue();
        result.Recommendation.Should().Be(BillingLifecycleRecommendedAction.Expire.ToString());
        result.ResultingStatus.Should().Be(OrganizationSubscriptionStatus.Expired.ToString());
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Expired);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Expired_Produces_NoAction_And_No_Mutation()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value, OrganizationSubscriptionStatus.Expired);
        SeedTenantAndSubscription(tenant, subscription);
        SeedInvoices(tenant.Id.Value, IssueOverdueInvoice(tenant.Id.Value, 200));

        ApplyOrganizationSubscriptionBillingDecisionResult result = await CreateHandler().Handle(
            new ApplyOrganizationSubscriptionBillingDecisionCommand(tenant.Id.Value), CancellationToken.None);

        result.TransitionApplied.Should().BeFalse();
        result.Recommendation.Should().Be(BillingLifecycleRecommendedAction.NoAction.ToString());
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Expired);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Canceled_Produces_NoAction_And_No_Mutation()
    {
        Tenant tenant = CreateActiveTenant();
        OrganizationSubscription subscription = CreateSubscription(tenant.Id.Value, OrganizationSubscriptionStatus.Canceled);
        SeedTenantAndSubscription(tenant, subscription);
        SeedInvoices(tenant.Id.Value, IssueOverdueInvoice(tenant.Id.Value, 200));

        ApplyOrganizationSubscriptionBillingDecisionResult result = await CreateHandler().Handle(
            new ApplyOrganizationSubscriptionBillingDecisionCommand(tenant.Id.Value), CancellationToken.None);

        result.TransitionApplied.Should().BeFalse();
        result.Recommendation.Should().Be(BillingLifecycleRecommendedAction.NoAction.ToString());
        subscription.Status.Should().Be(OrganizationSubscriptionStatus.Canceled);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new ApplyOrganizationSubscriptionBillingDecisionCommand(organizationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Subscription_Does_Not_Exist()
    {
        Tenant tenant = CreateActiveTenant();
        _tenants.GetByIdAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(tenant);
        _subscriptions.GetLatestForTenantAsync(tenant.Id.Value, Arg.Any<CancellationToken>()).Returns((OrganizationSubscription?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new ApplyOrganizationSubscriptionBillingDecisionCommand(tenant.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
