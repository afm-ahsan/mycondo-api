using AwesomeAssertions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetPlatformBillingSummary;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.GetPlatformBillingSummary;

public class GetPlatformBillingSummaryQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero); // ~12:00 Asia/Dhaka
    private static readonly DateOnly Today = new(2026, 8, 30);

    private readonly ISubscriptionInvoiceRepository _invoices = Substitute.For<ISubscriptionInvoiceRepository>();
    private readonly ISubscriptionPaymentRepository _payments = Substitute.For<ISubscriptionPaymentRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetPlatformBillingSummaryQueryHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
        _payments.SearchAsync(Arg.Any<Guid?>(), Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionPayment>());
        _invoices.GetOutstandingAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice>());
    }

    private GetPlatformBillingSummaryQueryHandler CreateHandler() => new(_invoices, _payments, _clock);

    private static SubscriptionInvoice IssueInvoice(string currency, decimal amount, DateOnly issueDate, DateOnly dueDate)
    {
        (SubscriptionInvoice invoice, _) = SubscriptionInvoice.Issue(
            Guid.NewGuid(), OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}",
            new DateOnly(issueDate.Year, issueDate.Month, 1), issueDate.AddDays(1), issueDate, dueDate, currency,
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, amount, 0m, amount, "Professional (Monthly)")],
            new DateTimeOffset(issueDate, TimeOnly.MinValue, TimeSpan.Zero));

        return invoice;
    }

    [Fact]
    public async Task Invoiced_Amount_Sums_TotalAmount_Of_Issued_Invoices_Grouped_By_Currency()
    {
        SubscriptionInvoice bdt1 = IssueInvoice("BDT", 5000m, new DateOnly(2026, 8, 5), new DateOnly(2026, 9, 5));
        SubscriptionInvoice bdt2 = IssueInvoice("BDT", 3000m, new DateOnly(2026, 8, 10), new DateOnly(2026, 9, 10));
        SubscriptionInvoice usd = IssueInvoice("USD", 100m, new DateOnly(2026, 8, 15), new DateOnly(2026, 9, 15));

        _invoices.GetIssuedInRangeAsync(null, Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { bdt1, bdt2, usd });

        IReadOnlyList<PlatformBillingSummaryCurrencyDto> result =
            await CreateHandler().Handle(new GetPlatformBillingSummaryQuery(), CancellationToken.None);

        result.Should().Contain(x => x.Currency == "BDT" && x.InvoicedAmount == 8000m && x.InvoicedInvoiceCount == 2);
        result.Should().Contain(x => x.Currency == "USD" && x.InvoicedAmount == 100m && x.InvoicedInvoiceCount == 1);
    }

    [Fact]
    public async Task Invoiced_Amount_Excludes_Void_Invoices_But_Includes_Canceled_Invoices()
    {
        SubscriptionInvoice voided = IssueInvoice("BDT", 5000m, new DateOnly(2026, 8, 5), new DateOnly(2026, 9, 5));
        voided.Void("Billed against the wrong subscription", NowUtc);

        SubscriptionInvoice canceled = IssueInvoice("BDT", 2000m, new DateOnly(2026, 8, 6), new DateOnly(2026, 9, 6));
        canceled.Cancel("Commercial waiver", NowUtc);

        _invoices.GetIssuedInRangeAsync(null, Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { voided, canceled });

        IReadOnlyList<PlatformBillingSummaryCurrencyDto> result =
            await CreateHandler().Handle(new GetPlatformBillingSummaryQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].InvoicedAmount.Should().Be(2000m); // only the Canceled invoice counts — Void never represented a valid charge
        result[0].InvoicedInvoiceCount.Should().Be(1);
    }

    [Fact]
    public async Task Collected_Amount_Sums_Payment_Amount_Never_Derived_From_Invoiced_Minus_Outstanding()
    {
        _invoices.GetIssuedInRangeAsync(null, Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice>());

        SubscriptionPayment payment1 = SubscriptionPayment.Record(
            Guid.NewGuid(), SubscriptionInvoiceId.New(), 1500m, "BDT", new DateOnly(2026, 8, 12), "REF-1", null, NowUtc);
        SubscriptionPayment payment2 = SubscriptionPayment.Record(
            Guid.NewGuid(), SubscriptionInvoiceId.New(), 500m, "BDT", new DateOnly(2026, 8, 20), "REF-2", null, NowUtc);

        _payments.SearchAsync(null, Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionPayment> { payment1, payment2 });

        IReadOnlyList<PlatformBillingSummaryCurrencyDto> result =
            await CreateHandler().Handle(new GetPlatformBillingSummaryQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].CollectedAmount.Should().Be(2000m);
        result[0].CollectedPaymentCount.Should().Be(2);
        // Invoiced/outstanding are independently zero here — collected is never Invoiced - Outstanding.
        result[0].InvoicedAmount.Should().Be(0m);
        result[0].OutstandingAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Outstanding_And_Overdue_Reuse_The_Current_Snapshot_With_No_Aging_Buckets()
    {
        _invoices.GetIssuedInRangeAsync(null, Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice>());

        SubscriptionInvoice overdue = IssueInvoice("BDT", 5000m, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 15));
        SubscriptionInvoice notYetDue = IssueInvoice("BDT", 1000m, new DateOnly(2026, 8, 20), new DateOnly(2026, 9, 20));

        _invoices.GetOutstandingAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { overdue, notYetDue });

        IReadOnlyList<PlatformBillingSummaryCurrencyDto> result =
            await CreateHandler().Handle(new GetPlatformBillingSummaryQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].OutstandingAmount.Should().Be(6000m);
        result[0].OutstandingInvoiceCount.Should().Be(2);
        result[0].OverdueOutstandingAmount.Should().Be(5000m);
        result[0].OverdueInvoiceCount.Should().Be(1);
        result[0].MaxDaysOverdue.Should().Be(Today.DayNumber - new DateOnly(2026, 3, 15).DayNumber);
    }

    [Fact]
    public async Task Keeps_Different_Currencies_As_Separate_Rows_Never_Summed_Together()
    {
        SubscriptionInvoice bdt = IssueInvoice("BDT", 5000m, new DateOnly(2026, 8, 5), new DateOnly(2026, 9, 5));
        SubscriptionInvoice usd = IssueInvoice("USD", 100m, new DateOnly(2026, 8, 5), new DateOnly(2026, 9, 5));

        _invoices.GetIssuedInRangeAsync(null, Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice> { bdt, usd });

        IReadOnlyList<PlatformBillingSummaryCurrencyDto> result =
            await CreateHandler().Handle(new GetPlatformBillingSummaryQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.Currency).Should().BeEquivalentTo(new[] { "BDT", "USD" });
    }

    [Fact]
    public async Task Passes_Organization_And_Date_Filters_Through_To_The_Invoiced_And_Collected_Queries()
    {
        Guid tenantId = Guid.NewGuid();
        DateOnly from = new(2026, 8, 1);
        DateOnly to = new(2026, 8, 31);

        _invoices.GetIssuedInRangeAsync(tenantId, from, to, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice>());
        _payments.SearchAsync(tenantId, from, to, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionPayment>());
        _invoices.GetOutstandingAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<SubscriptionInvoice>());

        await CreateHandler().Handle(
            new GetPlatformBillingSummaryQuery(tenantId, from, to), CancellationToken.None);

        await _invoices.Received(1).GetIssuedInRangeAsync(tenantId, from, to, Arg.Any<CancellationToken>());
        await _payments.Received(1).SearchAsync(tenantId, from, to, Arg.Any<CancellationToken>());
        await _invoices.Received(1).GetOutstandingAsync(tenantId, Arg.Any<CancellationToken>());
    }
}
