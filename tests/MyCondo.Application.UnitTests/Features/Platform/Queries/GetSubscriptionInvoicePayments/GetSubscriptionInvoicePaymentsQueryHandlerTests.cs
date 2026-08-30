using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetSubscriptionInvoicePayments;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.GetSubscriptionInvoicePayments;

public class GetSubscriptionInvoicePaymentsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly ISubscriptionInvoiceRepository _invoices = Substitute.For<ISubscriptionInvoiceRepository>();
    private readonly ISubscriptionPaymentRepository _payments = Substitute.For<ISubscriptionPaymentRepository>();

    private GetSubscriptionInvoicePaymentsQueryHandler CreateHandler() => new(_invoices, _payments);

    private static SubscriptionInvoice IssueInvoice()
    {
        (SubscriptionInvoice invoice, _) = SubscriptionInvoice.Issue(
            TenantId, OrganizationSubscriptionId.New(), $"SUBINV-{Guid.NewGuid():N}",
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), "BDT",
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1, BillingCycle.Monthly, 8000m, 0m, 8000m, "Professional (Monthly)")],
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
        return invoice;
    }

    [Fact]
    public async Task Returns_The_Payment_History_For_The_Invoice()
    {
        SubscriptionInvoice invoice = IssueInvoice();
        SubscriptionPayment payment = SubscriptionPayment.Record(
            TenantId, invoice.Id, 3000m, "BDT", new DateOnly(2026, 3, 5), "BANK-REF-1", null,
            new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero));

        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _payments.GetForInvoiceAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(new List<SubscriptionPayment> { payment });

        IReadOnlyList<PlatformSubscriptionPaymentDto> result =
            await CreateHandler().Handle(new GetSubscriptionInvoicePaymentsQuery(invoice.Id.Value), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Amount.Should().Be(3000m);
        result[0].ReferenceNumber.Should().Be("BANK-REF-1");
    }

    [Fact]
    public async Task Throws_NotFound_When_The_Invoice_Does_Not_Exist()
    {
        Guid missingId = Guid.NewGuid();
        _invoices.GetByIdAsync(Arg.Any<SubscriptionInvoiceId>(), Arg.Any<CancellationToken>())
            .Returns((SubscriptionInvoice?)null);

        Func<Task> act = async () =>
            await CreateHandler().Handle(new GetSubscriptionInvoicePaymentsQuery(missingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
