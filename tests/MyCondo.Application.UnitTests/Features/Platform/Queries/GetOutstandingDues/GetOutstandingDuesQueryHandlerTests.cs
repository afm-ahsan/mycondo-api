using AwesomeAssertions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetOutstandingDues;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.GetOutstandingDues;

public class GetOutstandingDuesQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero); // ~12:00 Asia/Dhaka
    private static readonly DateOnly Today = new(2026, 8, 30);

    private readonly ISubscriptionInvoiceRepository _invoices = Substitute.For<ISubscriptionInvoiceRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetOutstandingDuesQueryHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
        _tenants.GetNamesByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string>());
    }

    private GetOutstandingDuesQueryHandler CreateHandler() => new(_invoices, _tenants, _clock);

    private static SubscriptionInvoice IssueOutstandingInvoice(
        Guid tenantId, string currency, decimal outstanding, DateOnly dueDate)
    {
        (SubscriptionInvoice invoice, _) = SubscriptionInvoice.Issue(
            tenantId, OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}",
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), dueDate, currency,
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, outstanding, 0m, outstanding, "Professional (Monthly)")],
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));

        return invoice;
    }

    [Fact]
    public async Task Sums_Outstanding_Amount_Per_Organization_And_Currency()
    {
        Guid tenantId = Guid.NewGuid();
        SubscriptionInvoice first = IssueOutstandingInvoice(tenantId, "BDT", 5000m, new DateOnly(2026, 3, 15));
        SubscriptionInvoice second = IssueOutstandingInvoice(tenantId, "BDT", 3000m, new DateOnly(2026, 4, 1));

        _invoices.GetOutstandingAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { first, second });

        PagedResult<PlatformOrganizationOutstandingDto> result =
            await CreateHandler().Handle(new GetOutstandingDuesQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].OutstandingAmount.Should().Be(8000m);
        result.Items[0].OutstandingInvoiceCount.Should().Be(2);
        result.Items[0].OldestDueDate.Should().Be(new DateOnly(2026, 3, 15));
    }

    [Fact]
    public async Task Keeps_Different_Currencies_For_The_Same_Organization_As_Separate_Rows()
    {
        Guid tenantId = Guid.NewGuid();
        SubscriptionInvoice bdt = IssueOutstandingInvoice(tenantId, "BDT", 5000m, new DateOnly(2026, 3, 15));
        SubscriptionInvoice usd = IssueOutstandingInvoice(tenantId, "USD", 100m, new DateOnly(2026, 3, 15));

        _invoices.GetOutstandingAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { bdt, usd });

        PagedResult<PlatformOrganizationOutstandingDto> result =
            await CreateHandler().Handle(new GetOutstandingDuesQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(x => x.Currency == "BDT" && x.OutstandingAmount == 5000m);
        result.Items.Should().Contain(x => x.Currency == "USD" && x.OutstandingAmount == 100m);
        result.Items.Sum(x => x.OutstandingAmount).Should().Be(5100m); // never actually spent together — different currencies
    }

    [Fact]
    public async Task MaxDaysOverdue_Is_Null_When_Nothing_In_The_Group_Is_Overdue()
    {
        Guid tenantId = Guid.NewGuid();
        SubscriptionInvoice notYetDue = IssueOutstandingInvoice(tenantId, "BDT", 5000m, new DateOnly(2026, 9, 15));

        _invoices.GetOutstandingAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { notYetDue });

        PagedResult<PlatformOrganizationOutstandingDto> result =
            await CreateHandler().Handle(new GetOutstandingDuesQuery(), CancellationToken.None);

        result.Items[0].MaxDaysOverdue.Should().BeNull();
    }

    [Fact]
    public async Task MaxDaysOverdue_Reflects_The_Most_Overdue_Invoice_In_The_Group()
    {
        Guid tenantId = Guid.NewGuid();
        SubscriptionInvoice moreOverdue = IssueOutstandingInvoice(tenantId, "BDT", 5000m, new DateOnly(2026, 3, 1));
        SubscriptionInvoice lessOverdue = IssueOutstandingInvoice(tenantId, "BDT", 1000m, new DateOnly(2026, 6, 1));

        _invoices.GetOutstandingAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { moreOverdue, lessOverdue });

        PagedResult<PlatformOrganizationOutstandingDto> result =
            await CreateHandler().Handle(new GetOutstandingDuesQuery(), CancellationToken.None);

        result.Items[0].MaxDaysOverdue.Should().Be(Today.DayNumber - new DateOnly(2026, 3, 1).DayNumber);
    }

    [Fact]
    public async Task Filters_By_Organization_When_OrganizationId_Is_Supplied()
    {
        Guid tenantId = Guid.NewGuid();
        _invoices.GetOutstandingAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice>());

        await CreateHandler().Handle(new GetOutstandingDuesQuery(OrganizationId: tenantId), CancellationToken.None);

        await _invoices.Received(1).GetOutstandingAsync(tenantId, Arg.Any<CancellationToken>());
    }
}
