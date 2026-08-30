using AwesomeAssertions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.ListSubscriptionInvoices;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.ListSubscriptionInvoices;

public class ListSubscriptionInvoicesQueryHandlerTests
{
    // IClock is mocked below, so this fixed "now" is independent of when the suite actually runs.
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero); // ~12:00 Asia/Dhaka
    private static readonly DateOnly Today = new(2026, 8, 30);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly ISubscriptionInvoiceRepository _invoices = Substitute.For<ISubscriptionInvoiceRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ListSubscriptionInvoicesQueryHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private ListSubscriptionInvoicesQueryHandler CreateHandler() => new(_invoices, _tenants, _clock);

    private static SubscriptionInvoice IssueInvoice(DateOnly dueDate, decimal outstanding = 8000m)
    {
        (SubscriptionInvoice invoice, _) = SubscriptionInvoice.Issue(
            TenantId, OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}",
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), dueDate, "BDT",
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, 8000m, 0m, 8000m, "Professional (Monthly)")],
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));

        if (outstanding < invoice.TotalAmount)
        {
            invoice.ApplyPayment(invoice.TotalAmount - outstanding, "BDT", new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero));
        }

        return invoice;
    }

    [Fact]
    public async Task Maps_Invoices_With_Organization_Name_And_Days_Overdue()
    {
        SubscriptionInvoice overdue = IssueInvoice(dueDate: new DateOnly(2026, 3, 15));

        _invoices.SearchAsync(1, 20, null, null, null, null, null, Today, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<SubscriptionInvoice>([overdue], 1, 20, 1));
        _tenants.GetNamesByIdsAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(TenantId)), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { [TenantId] = "Akter Residence Park" });

        PagedResult<PlatformSubscriptionInvoiceListItemDto> result =
            await CreateHandler().Handle(new ListSubscriptionInvoicesQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle();
        PlatformSubscriptionInvoiceListItemDto item = result.Items[0];
        item.OrganizationName.Should().Be("Akter Residence Park");
        item.TenantId.Should().Be(TenantId);
        item.Status.Should().Be(nameof(SubscriptionInvoiceStatus.Issued));
        item.DaysOverdue.Should().Be(Today.DayNumber - new DateOnly(2026, 3, 15).DayNumber);
    }

    [Fact]
    public async Task Defaults_Organization_Name_To_Unknown_When_Tenant_Lookup_Misses()
    {
        SubscriptionInvoice invoice = IssueInvoice(dueDate: new DateOnly(2026, 4, 1));

        _invoices.SearchAsync(1, 20, null, null, null, null, null, Today, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<SubscriptionInvoice>([invoice], 1, 20, 1));
        _tenants.GetNamesByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string>());

        PagedResult<PlatformSubscriptionInvoiceListItemDto> result =
            await CreateHandler().Handle(new ListSubscriptionInvoicesQuery(), CancellationToken.None);

        result.Items[0].OrganizationName.Should().Be("Unknown");
    }

    [Fact]
    public async Task Parses_The_Status_Filter_And_Passes_Todays_Business_Date_Before_Searching()
    {
        _invoices.SearchAsync(1, 20, null, SubscriptionInvoiceStatus.Paid, null, null, null, Today, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<SubscriptionInvoice>([], 1, 20, 0));
        _tenants.GetNamesByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string>());

        await CreateHandler().Handle(new ListSubscriptionInvoicesQuery(Status: "Paid"), CancellationToken.None);

        await _invoices.Received(1).SearchAsync(
            1, 20, null, SubscriptionInvoiceStatus.Paid, null, null, null, Today, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_Not_Yet_Due_Invoice_Has_No_Days_Overdue()
    {
        SubscriptionInvoice invoice = IssueInvoice(dueDate: new DateOnly(2026, 9, 15));

        _invoices.SearchAsync(1, 20, null, null, null, null, null, Today, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<SubscriptionInvoice>([invoice], 1, 20, 1));
        _tenants.GetNamesByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string>());

        PagedResult<PlatformSubscriptionInvoiceListItemDto> result =
            await CreateHandler().Handle(new ListSubscriptionInvoicesQuery(), CancellationToken.None);

        result.Items[0].DaysOverdue.Should().BeNull();
    }
}
