using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetSubscriptionInvoiceById;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.GetSubscriptionInvoiceById;

public class GetSubscriptionInvoiceByIdQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 30, 6, 0, 0, TimeSpan.Zero); // ~12:00 Asia/Dhaka
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly ISubscriptionInvoiceRepository _invoices = Substitute.For<ISubscriptionInvoiceRepository>();
    private readonly ISubscriptionPaymentRepository _payments = Substitute.For<ISubscriptionPaymentRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetSubscriptionInvoiceByIdQueryHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private GetSubscriptionInvoiceByIdQueryHandler CreateHandler() => new(_invoices, _payments, _tenants, _clock);

    private static (SubscriptionInvoice Invoice, IReadOnlyList<SubscriptionInvoiceLine> Lines) IssueInvoice(DateOnly dueDate)
    {
        return SubscriptionInvoice.Issue(
            TenantId, OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}",
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), dueDate, "BDT",
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, 8000m, 0m, 8000m, "Professional (Monthly)")],
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Returns_Detail_With_Lines_Payments_And_Days_Overdue()
    {
        (SubscriptionInvoice invoice, IReadOnlyList<SubscriptionInvoiceLine> lines) = IssueInvoice(new DateOnly(2026, 3, 15));
        SubscriptionPayment payment = SubscriptionPayment.Record(
            TenantId, invoice.Id, 2000m, "BDT", new DateOnly(2026, 3, 5), "BANK-REF-1", "Partial",
            new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero));

        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _invoices.GetLinesAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(lines);
        _payments.GetForInvoiceAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(new List<SubscriptionPayment> { payment });
        _tenants.GetByIdAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(Tenant.Provision("ARP", "arp", NowUtc));

        PlatformSubscriptionInvoiceDetailDto result =
            await CreateHandler().Handle(new GetSubscriptionInvoiceByIdQuery(invoice.Id.Value), CancellationToken.None);

        result.InvoiceId.Should().Be(invoice.Id.Value);
        result.Lines.Should().ContainSingle();
        result.Payments.Should().ContainSingle();
        result.Payments[0].ReferenceNumber.Should().Be("BANK-REF-1");
        result.DaysOverdue.Should().Be(new DateOnly(2026, 8, 30).DayNumber - new DateOnly(2026, 3, 15).DayNumber);
    }

    [Fact]
    public async Task Throws_NotFound_When_The_Invoice_Does_Not_Exist()
    {
        Guid missingId = Guid.NewGuid();
        _invoices.GetByIdAsync(Arg.Any<SubscriptionInvoiceId>(), Arg.Any<CancellationToken>())
            .Returns((SubscriptionInvoice?)null);

        Func<Task> act = async () => await CreateHandler().Handle(new GetSubscriptionInvoiceByIdQuery(missingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
