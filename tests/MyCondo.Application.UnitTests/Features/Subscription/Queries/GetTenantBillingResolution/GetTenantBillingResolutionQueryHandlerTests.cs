using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Subscription.DTOs;
using MyCondo.Application.Features.Subscription.Queries.GetTenantBillingResolution;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Subscription.Queries.GetTenantBillingResolution;

public class GetTenantBillingResolutionQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero); // ~12:00 Asia/Dhaka
    private static readonly DateOnly Today = new(2026, 8, 30);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IOrganizationSubscriptionRepository _subscriptions = Substitute.For<IOrganizationSubscriptionRepository>();
    private readonly ISubscriptionInvoiceRepository _invoices = Substitute.For<ISubscriptionInvoiceRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetTenantBillingResolutionQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
        _invoices.GetOutstandingAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice>());
    }

    private GetTenantBillingResolutionQueryHandler CreateHandler() => new(_currentUser, _subscriptions, _invoices, _clock);

    private static SubscriptionInvoice IssueOutstandingInvoice(string currency, decimal outstanding, DateOnly dueDate) =>
        SubscriptionInvoice.Issue(
            TenantId, OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}",
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), dueDate, currency,
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, outstanding, 0m, outstanding, "Professional (Monthly)")],
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)).Invoice;

    [Fact]
    public async Task No_Tenant_Context_Throws_Forbidden()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await CreateHandler().Handle(new GetTenantBillingResolutionQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Resolves_Outstanding_Invoices_Using_Only_The_Callers_Own_Tenant()
    {
        await CreateHandler().Handle(new GetTenantBillingResolutionQuery(), CancellationToken.None);

        await _invoices.Received(1).GetOutstandingAsync(TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reports_Null_Subscription_Status_When_No_Subscription_Was_Ever_Provisioned()
    {
        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns((OrganizationSubscription?)null);
        _subscriptions.GetLatestForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns((OrganizationSubscription?)null);

        TenantBillingResolutionDto result = await CreateHandler().Handle(new GetTenantBillingResolutionQuery(), CancellationToken.None);

        result.SubscriptionStatus.Should().BeNull();
    }

    [Fact]
    public async Task Falls_Back_To_The_Latest_Subscription_When_No_Current_Row_Exists()
    {
        OrganizationSubscription latest = OrganizationSubscription.Create(
            TenantId, SubscriptionPackageVersionId.New(), BillingCycle.Monthly, new DateOnly(2026, 1, 1), null, null,
            1000m, 0m, "BDT", NowUtc, autoRenew: false);
        latest.Cancel(NowUtc);

        _subscriptions.GetCurrentForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns((OrganizationSubscription?)null);
        _subscriptions.GetLatestForTenantAsync(TenantId, Arg.Any<CancellationToken>()).Returns(latest);

        TenantBillingResolutionDto result = await CreateHandler().Handle(new GetTenantBillingResolutionQuery(), CancellationToken.None);

        result.SubscriptionStatus.Should().Be("Canceled");
    }

    [Fact]
    public async Task Sums_Outstanding_Amount_Per_Currency_And_Reports_Backend_Derived_DaysOverdue()
    {
        SubscriptionInvoice first = IssueOutstandingInvoice("BDT", 5000m, new DateOnly(2026, 3, 15));
        SubscriptionInvoice second = IssueOutstandingInvoice("BDT", 3000m, new DateOnly(2026, 4, 1));
        _invoices.GetOutstandingAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { first, second });

        TenantBillingResolutionDto result = await CreateHandler().Handle(new GetTenantBillingResolutionQuery(), CancellationToken.None);

        result.OutstandingSummary.Should().ContainSingle();
        result.OutstandingSummary[0].Currency.Should().Be("BDT");
        result.OutstandingSummary[0].OutstandingAmount.Should().Be(8000m);
        result.OutstandingSummary[0].OutstandingInvoiceCount.Should().Be(2);

        result.OutstandingInvoices.Should().HaveCount(2);
        result.OutstandingInvoices.Should().Contain(
            x => x.DueDate == new DateOnly(2026, 3, 15) && x.DaysOverdue == Today.DayNumber - new DateOnly(2026, 3, 15).DayNumber);
    }

    [Fact]
    public async Task Keeps_Different_Currencies_As_Separate_Summary_Rows_Never_Summed_Together()
    {
        SubscriptionInvoice bdt = IssueOutstandingInvoice("BDT", 5000m, new DateOnly(2026, 3, 15));
        SubscriptionInvoice usd = IssueOutstandingInvoice("USD", 100m, new DateOnly(2026, 3, 15));
        _invoices.GetOutstandingAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { bdt, usd });

        TenantBillingResolutionDto result = await CreateHandler().Handle(new GetTenantBillingResolutionQuery(), CancellationToken.None);

        result.OutstandingSummary.Should().HaveCount(2);
        result.OutstandingSummary.Should().Contain(x => x.Currency == "BDT" && x.OutstandingAmount == 5000m);
        result.OutstandingSummary.Should().Contain(x => x.Currency == "USD" && x.OutstandingAmount == 100m);
    }

    [Fact]
    public async Task Not_Yet_Due_Invoice_Reports_Null_DaysOverdue()
    {
        SubscriptionInvoice notYetDue = IssueOutstandingInvoice("BDT", 5000m, new DateOnly(2026, 9, 15));
        _invoices.GetOutstandingAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { notYetDue });

        TenantBillingResolutionDto result = await CreateHandler().Handle(new GetTenantBillingResolutionQuery(), CancellationToken.None);

        result.OutstandingInvoices[0].DaysOverdue.Should().BeNull();
    }
}
