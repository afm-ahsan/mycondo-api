using AwesomeAssertions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetOrganizationBillingHistory;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.GetOrganizationBillingHistory;

public class GetOrganizationBillingHistoryQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero); // ~12:00 Asia/Dhaka
    private static readonly DateOnly Today = new(2026, 8, 30);

    private readonly ISubscriptionInvoiceRepository _invoices = Substitute.For<ISubscriptionInvoiceRepository>();
    private readonly ISubscriptionPaymentRepository _payments = Substitute.For<ISubscriptionPaymentRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetOrganizationBillingHistoryQueryHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private GetOrganizationBillingHistoryQueryHandler CreateHandler() => new(_invoices, _payments, _clock);

    private SubscriptionInvoice IssueInvoice(DateOnly issueDate, DateOnly dueDate, decimal amount = 5000m)
    {
        (SubscriptionInvoice invoice, _) = SubscriptionInvoice.Issue(
            _tenantId, OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}",
            new DateOnly(issueDate.Year, issueDate.Month, 1), issueDate.AddDays(1), issueDate, dueDate, "BDT",
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, amount, 0m, amount, "Professional (Monthly)")],
            new DateTimeOffset(issueDate, TimeOnly.MinValue, TimeSpan.Zero));

        return invoice;
    }

    private void SetupEmptyRepositories()
    {
        _invoices.GetIssuedInRangeAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice>());
        _payments.SearchAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionPayment>());
    }

    [Fact]
    public async Task Merges_Invoice_And_Payment_Events_Ordered_Newest_First()
    {
        SubscriptionInvoice invoice = IssueInvoice(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 15));
        SubscriptionPayment payment = SubscriptionPayment.Record(
            _tenantId, invoice.Id, 5000m, "BDT", new DateOnly(2026, 7, 20), "REF-1", null, NowUtc);

        _invoices.GetIssuedInRangeAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { invoice });
        _payments.SearchAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionPayment> { payment });

        PagedResult<OrganizationBillingHistoryEventDto> result = await CreateHandler().Handle(
            new GetOrganizationBillingHistoryQuery(_tenantId), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].EventType.Should().Be("PaymentRecorded"); // 2026-07-20, newer than the invoice's 2026-07-01
        result.Items[0].EventDate.Should().Be(new DateOnly(2026, 7, 20));
        result.Items[1].EventType.Should().Be("InvoiceIssued");
        result.Items[1].EventDate.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public async Task Payment_Event_Resolves_Invoice_Number_From_The_Full_Invoice_Set()
    {
        SubscriptionInvoice invoice = IssueInvoice(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 15));
        SubscriptionPayment payment = SubscriptionPayment.Record(
            _tenantId, invoice.Id, 5000m, "BDT", new DateOnly(2026, 8, 25), "REF-2", null, NowUtc);

        // The invoice itself falls outside the requested display window, but the payment settling it
        // does not — invoice-number resolution must not depend on the invoice event being in range.
        _invoices.GetIssuedInRangeAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { invoice });
        _payments.SearchAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionPayment> { payment });

        PagedResult<OrganizationBillingHistoryEventDto> result = await CreateHandler().Handle(
            new GetOrganizationBillingHistoryQuery(_tenantId, DateFrom: new DateOnly(2026, 8, 1)), CancellationToken.None);

        result.Items.Should().ContainSingle(); // the invoice-issued event is filtered out by DateFrom
        result.Items[0].EventType.Should().Be("PaymentRecorded");
        result.Items[0].InvoiceNumber.Should().Be(invoice.InvoiceNumber);
    }

    [Fact]
    public async Task Date_Filter_Applies_Independently_To_Each_Event_Types_Own_Date()
    {
        SubscriptionInvoice inRangeInvoice = IssueInvoice(new DateOnly(2026, 8, 10), new DateOnly(2026, 9, 10));
        SubscriptionInvoice outOfRangeInvoice = IssueInvoice(new DateOnly(2026, 5, 10), new DateOnly(2026, 6, 10));
        SubscriptionPayment inRangePayment = SubscriptionPayment.Record(
            _tenantId, outOfRangeInvoice.Id, 5000m, "BDT", new DateOnly(2026, 8, 12), "REF-3", null, NowUtc);

        _invoices.GetIssuedInRangeAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { inRangeInvoice, outOfRangeInvoice });
        _payments.SearchAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionPayment> { inRangePayment });

        PagedResult<OrganizationBillingHistoryEventDto> result = await CreateHandler().Handle(
            new GetOrganizationBillingHistoryQuery(_tenantId, DateFrom: new DateOnly(2026, 8, 1), DateTo: new DateOnly(2026, 8, 31)),
            CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(x => x.EventType == "InvoiceIssued" && x.InvoiceId == inRangeInvoice.Id.Value);
        result.Items.Should().Contain(x => x.EventType == "PaymentRecorded" && x.InvoiceId == outOfRangeInvoice.Id.Value);
    }

    [Fact]
    public async Task Invoice_Events_Carry_DaysOverdue_And_Payment_Events_Never_Do()
    {
        SubscriptionInvoice overdueInvoice = IssueInvoice(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 15));
        SubscriptionPayment payment = SubscriptionPayment.Record(
            _tenantId, overdueInvoice.Id, 1000m, "BDT", new DateOnly(2026, 4, 1), "REF-4", null, NowUtc);

        _invoices.GetIssuedInRangeAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { overdueInvoice });
        _payments.SearchAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionPayment> { payment });

        PagedResult<OrganizationBillingHistoryEventDto> result = await CreateHandler().Handle(
            new GetOrganizationBillingHistoryQuery(_tenantId), CancellationToken.None);

        OrganizationBillingHistoryEventDto invoiceEvent = result.Items.Single(x => x.EventType == "InvoiceIssued");
        invoiceEvent.DaysOverdue.Should().Be(Today.DayNumber - new DateOnly(2026, 3, 15).DayNumber);

        OrganizationBillingHistoryEventDto paymentEvent = result.Items.Single(x => x.EventType == "PaymentRecorded");
        paymentEvent.DaysOverdue.Should().BeNull();
    }

    [Fact]
    public async Task Paginates_The_Merged_Timeline_In_Memory()
    {
        List<SubscriptionInvoice> invoices = Enumerable.Range(0, 5)
            .Select(i => IssueInvoice(new DateOnly(2026, 1, 1).AddMonths(i), new DateOnly(2026, 1, 15).AddMonths(i)))
            .ToList();

        _invoices.GetIssuedInRangeAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(invoices);
        _payments.SearchAsync(_tenantId, null, null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionPayment>());

        PagedResult<OrganizationBillingHistoryEventDto> result = await CreateHandler().Handle(
            new GetOrganizationBillingHistoryQuery(_tenantId, Page: 2, PageSize: 2), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(5);
        result.Page.Should().Be(2);
    }
}
