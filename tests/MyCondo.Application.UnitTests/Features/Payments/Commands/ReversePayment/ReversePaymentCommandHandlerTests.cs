using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Application.Features.Payments.Commands.ReversePayment;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.PaymentAllocations;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Payments.Payments.Exceptions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Payments.Commands.ReversePayment;

/// <summary>
/// Handler-level tests, same NSubstitute pattern as ConfirmBookingPaymentCommandHandlerTests —
/// this is the regression suite for the UX-3 payment/invoice consistency fix: proving reversal
/// restores every invoice a payment's FIFO allocations were applied to, not just the payment/ledger
/// side, and that a double reversal is rejected before any invoice is touched.
/// </summary>
public class ReversePaymentCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IPaymentAllocationRepository _paymentAllocations = Substitute.For<IPaymentAllocationRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ReversePaymentCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Substitute.For<IUnitOfWorkTransaction>());
        StubFinancialPosting();
    }

    /// <summary>Makes the mocked <see cref="IFinancialPostingService"/> behave like the real one — see
    /// VoidInvoiceCommandHandlerTests for why.</summary>
    private void StubFinancialPosting() =>
        _financialPosting.PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                FinancialPostingRequest request = callInfo.Arg<FinancialPostingRequest>();
                List<LedgerLine> lines = request.Lines
                    .Select(l => new LedgerLine(l.Role, l.FlatId, l.Direction, l.Amount, l.LineDescription ?? request.Description))
                    .ToList();
                (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
                    request.TenantId, request.BusinessDate, request.Description, request.PostingPurpose,
                    request.SourceId, lines, Now);
                return new FinancialPostingResult(posting, entries);
            });

    private ReversePaymentCommandHandler CreateHandler() => new(
        _payments, _paymentAllocations, _invoices, _financialPosting, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<ReversePaymentCommandHandler>>());

    private static Invoice IssuedInvoice(string invoiceNumber, decimal totalAmount)
    {
        InvoiceLineInput line = new(null, "Service Charge", "Maintenance", "FixedAmount", totalAmount, null, 1, totalAmount, "Test line");
        (Invoice invoice, _) = Invoice.Issue(
            TenantId, BuildingId, FlatId, invoiceNumber, InvoiceSource.ServiceCharge,
            Today, Today, Today, Today, [line], LedgerPostingId.New(), Now);
        return invoice;
    }

    private static Payment PostedPayment(decimal amount, Guid? tenantId = null) => Payment.Record(
        tenantId ?? TenantId, FlatId, amount, PaymentMethod.Cash, "REF-1", Today, null, LedgerPostingId.New(), Now);

    [Fact]
    public async Task Full_Payment_Reversal_Restores_Invoice_To_Issued()
    {
        Invoice invoice = IssuedInvoice("INV-0001", 1000m);
        invoice.ApplyPayment(1000m); // now Paid
        Payment payment = PostedPayment(1000m);
        PaymentAllocation allocation = PaymentAllocation.Allocate(TenantId, payment.Id, invoice.Id, FlatId, 1000m, Now);

        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _paymentAllocations.GetForPaymentAsync(payment.Id, Arg.Any<CancellationToken>()).Returns([allocation]);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        PaymentDto result = await CreateHandler().Handle(
            new ReversePaymentCommand(payment.Id.Value, "Refunded to resident"), CancellationToken.None);

        result.Status.Should().Be("Reversed");
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.AmountPaid.Should().Be(0m);
        result.Allocations.Should().ContainSingle(a => a.InvoiceId == invoice.Id.Value && a.InvoiceNumber == "INV-0001");
    }

    [Fact]
    public async Task Reversing_The_Second_Of_Two_Payments_Leaves_Invoice_PartiallyPaid()
    {
        Invoice invoice = IssuedInvoice("INV-0002", 1000m);
        invoice.ApplyPayment(400m); // first payment, not reversed in this test
        invoice.ApplyPayment(600m); // second payment, now Paid

        Payment secondPayment = PostedPayment(600m);
        PaymentAllocation allocation = PaymentAllocation.Allocate(TenantId, secondPayment.Id, invoice.Id, FlatId, 600m, Now);

        _payments.GetByIdAsync(secondPayment.Id, Arg.Any<CancellationToken>()).Returns(secondPayment);
        _paymentAllocations.GetForPaymentAsync(secondPayment.Id, Arg.Any<CancellationToken>()).Returns([allocation]);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        await CreateHandler().Handle(new ReversePaymentCommand(secondPayment.Id.Value, "Bounced cheque"), CancellationToken.None);

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.AmountPaid.Should().Be(400m);
    }

    [Fact]
    public async Task Reversal_Restores_All_Invoices_A_Payment_Was_Allocated_Across()
    {
        Invoice invoiceA = IssuedInvoice("INV-0003", 300m);
        Invoice invoiceB = IssuedInvoice("INV-0004", 500m);
        invoiceA.ApplyPayment(300m);
        invoiceB.ApplyPayment(200m); // partially paid by this same payment

        Payment payment = PostedPayment(500m);
        PaymentAllocation allocationA = PaymentAllocation.Allocate(TenantId, payment.Id, invoiceA.Id, FlatId, 300m, Now);
        PaymentAllocation allocationB = PaymentAllocation.Allocate(TenantId, payment.Id, invoiceB.Id, FlatId, 200m, Now);

        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        _paymentAllocations.GetForPaymentAsync(payment.Id, Arg.Any<CancellationToken>()).Returns([allocationA, allocationB]);
        _invoices.GetByIdAsync(invoiceA.Id, Arg.Any<CancellationToken>()).Returns(invoiceA);
        _invoices.GetByIdAsync(invoiceB.Id, Arg.Any<CancellationToken>()).Returns(invoiceB);

        PaymentDto result = await CreateHandler().Handle(
            new ReversePaymentCommand(payment.Id.Value, "Duplicate entry"), CancellationToken.None);

        invoiceA.Status.Should().Be(InvoiceStatus.Issued);
        invoiceA.AmountPaid.Should().Be(0m);
        invoiceB.Status.Should().Be(InvoiceStatus.Issued);
        invoiceB.AmountPaid.Should().Be(0m);
        result.Allocations.Should().HaveCount(2);
    }

    [Fact]
    public async Task Double_Reversal_Is_Rejected_And_Invoice_Is_Not_Touched_Again()
    {
        Invoice invoice = IssuedInvoice("INV-0005", 1000m);
        invoice.ApplyPayment(1000m);
        Payment payment = PostedPayment(1000m);
        payment.Reverse("First reversal", null, Now); // simulate an already-reversed payment

        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        Func<Task> act = () => CreateHandler().Handle(
            new ReversePaymentCommand(payment.Id.Value, "Second attempt"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<PaymentAlreadyReversedException>();
        await _paymentAllocations.DidNotReceive().GetForPaymentAsync(Arg.Any<PaymentId>(), Arg.Any<CancellationToken>());
        invoice.Status.Should().Be(InvoiceStatus.Paid); // untouched — the guard fires before any invoice lookup
    }

    [Fact]
    public async Task Reversing_A_Payment_From_A_Different_Tenant_Is_Not_Found()
    {
        Payment payment = PostedPayment(500m, tenantId: Guid.NewGuid());
        _payments.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        Func<Task> act = () => CreateHandler().Handle(
            new ReversePaymentCommand(payment.Id.Value, "Wrong tenant"), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
